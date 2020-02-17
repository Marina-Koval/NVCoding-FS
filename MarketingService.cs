using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MoreLinq;
using NewDB;
using Portal.Web.Infrastructure.Enums.Notifications;
using CloudinaryAPI;
using Portal.Repos.DTO.LanguagesDtos;
using Portal.Web.Infrastructure.Helpers.Localizations;
using Portal.Web.Models._ViewModels.Marketing;

namespace Portal.Web.Services
{
    public class MarketingService : ApplicationService
    {
        private readonly PortalModel _db;
        private readonly string HttpsStringStart = "https://";
        private readonly decimal DefaultReasleProvision = 10M;

        public MarketingService(PortalModel db, UnitOfWork uow) : base(uow, db)
        {
            _db = db;
        }

        public async Task<ProductAboutViewModel> GetProductAbout(Guid productId)
        {
            Products product = await Repo.ProductsRepository.GetByIdAsync(productId);

            if (product is MasterCourses)
            {
                MasterCourses masterCourse = product as MasterCourses;

                ProductAboutViewModel result = new ProductAboutViewModel();

                result.Categories = masterCourse.ProductsCategories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Categories?.Name ?? string.Empty
                }).ToList();

                result.Tags = masterCourse.Tags;/*.Split(",".ToCharArray()[0]).ToList();*/
                result.WallpaperImageUrl = CloudinaryHelper.GetImageUrl(masterCourse.WallPaperUrl);
                result.WistiaProjectId = product.Companies.WistiaProjectId;

                result.Descriptions = masterCourse.MasterCoursesDescription.Select(mcd => new ProductDescription
                {
                    Name = mcd.Name,
                    Description = HttpUtility.HtmlDecode(mcd.LongDescription),
                    CoverImageUrl = CloudinaryHelper.GetImageUrl(mcd.CoverUrl),
                    IntroMovieUrl = mcd.IntroVideoUrl,
                    TrailerMovieUrl = mcd.TrailerVideoUrl,
                    LanguageId = mcd.Languages.Id,
                    Language = mcd.Languages.Name
                }).ToList();

                result.Languages = masterCourse.MasterCoursesDescription.Select(mcd => mcd.Languages).Select(l => new LanguagesDto
                {
                    Id = l.Id,
                    Image = l.Image,
                    LangCode = l.LangCode,
                    Name = l.Name
                }).ToList();
                
                return result;
            }
            //else if (product is Lectures)
            else
            {
                Lectures lecture = product as Lectures;

                ProductAboutViewModel result = new ProductAboutViewModel();

                result.Categories = lecture.ProductsCategories.Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Categories?.Name ?? string.Empty
                }).ToList();

                result.Tags = lecture.Tags;/*.Split(",".ToCharArray()[0]).ToList();*/
                result.WallpaperImageUrl = CloudinaryHelper.GetImageUrl(lecture.WallpaperUrl);

                result.Descriptions.Add(new ProductDescription
                {
                    CoverImageUrl = CloudinaryHelper.GetImageUrl(lecture.CoverPhoto),
                    TrailerMovieUrl = lecture.TrailerVideoUrl,
                    Description = HttpUtility.HtmlDecode(lecture.Description),
                    Name = lecture.Name,
                    LanguageId = LocalizationHelper.DefaultId,
                    Language = LocalizationHelper.Default
                });

                return result;
            }
        }

        public async Task<ProductPlaceViewModel> GetProductDistribution(Guid productId)
        {
            Products product = UnitOfWork.ProductsService.GetProductById(productId);

            ProductPlaceViewModel result = new ProductPlaceViewModel();

            result.ProductId = productId;
            result.IsLive = product.ProductStatus;
            result.IsLivePossible = Repo.ProductPricesRepository
                .Get(pp => pp.ProductId == productId).Any() && (Repo.ProductsRepository.Get(p => p.Id == productId)
                .FirstOrDefault()?.IsReady ?? false);

            result.WebsitePublishing = product.ShowOnPublicPage;
            result.MarketPlacePublishing = product.ShowOnMarketplace;

            result.AnyoneResellingDistribution = product.IsDistributionOn ?? false;
            result.PartnersResellingDistribution = product.IsPartnersDistributionOn;
            result.ResaleOn = (result.AnyoneResellingDistribution || result.PartnersResellingDistribution);

            result.DefaultResellingProvision =
                product.DefaultProvision != null ? (decimal)product.DefaultProvision.Value : DefaultReasleProvision;
            result.PartnersList = new List<ResellingDto>();
            result.ExistingPartners = new List<ResellingPartnerDto>();

            if (product.IsPartnersDistributionOn)
            {
                List<ProductReselling> resellingList = UnitOfWork.ProductResellingService.GetAllCourseReselling(productId);

                foreach (ProductReselling item in resellingList)
                {
                    result.PartnersList.Add(ResellingDto.Map(item));

                    result.ExistingPartners.Add(new ResellingPartnerDto
                    {
                        Id = item.Companies.Id,
                        Logo = CloudinaryHelper.GetImageUrl(item.Companies.LogoUrl),
                        Name = item.Companies.Name
                    });
                }
            }

            result.PossiblePartners =
                await UnitOfWork.ProductResellingService.GetPossibleResellingPartners(productId, this.GetCompany().Id, result.ExistingPartners);

            return result;
        }

        public async Task ProcessProductDistribution(ProductPlaceViewModel model)
        {
            Products product = await Repo.ProductsRepository.GetByIdAsync(model.ProductId);

            product.IsDistributionOn = model.AnyoneResellingDistribution;

            if (model.AnyoneResellingDistribution)
            {
                product.DefaultProvision = (double) model.DefaultResellingProvision;
            }
            else
            {
                product.DefaultProvision = null;

                var defaultResellingForRemoving = await Repo.ProductResellingRepository
                    .GetAsync(p => p.ProductId == model.ProductId && !p.IsPartnerResale);

                foreach (var item in defaultResellingForRemoving)
                {
                    Repo.ProductResellingRepository.Delete(item);
                }
            }

            product.IsPartnersDistributionOn = model.PartnersResellingDistribution;

            if (model.PartnersResellingDistribution)
            {
                var existingResellings = await Repo.ProductResellingRepository
                    .GetAsync(pr => pr.ProductId == model.ProductId);
                List<ResellingDto> existingConverted = new List<ResellingDto>();

                foreach (ProductReselling item in existingResellings)
                {
                    existingConverted.Add(ResellingDto.Map(item));
                }

                List<ResellingDto> adding = model.PartnersList?.Where(rp => rp.ExistenceStatus == 0).ToList();

                List<ResellingDto> removing = null;

                if (model.PartnersList != null)
                {
                    removing =
                        existingConverted.ExceptBy(model.PartnersList, r => r.PartnerId).ToList();
                }
                else
                {
                    removing =
                        existingConverted;
                }

                List<ResellingDto> editing = model.PartnersList?.Where(rp => rp.WasChanged).ToList();

                if (adding != null)
                {
                    foreach (var item in adding)
                    {
                        ProductReselling reselling = ResellingDto.MapBack(item, model.ProductId);

                        Repo.ProductResellingRepository.Insert(ref reselling);

                        Companies company = Repo.CompaniesRepository.GetById(item.PartnerId);

                        UnitOfWork.NotificationsService.CreateNewNotification(
                            (int) ENotificationClasses.Library,
                            (int) ENotificationTypes.CourseAddedToResellingToCompany,
                            company.OwnerId ?? GetUserProfileModel().Id, false, reselling.Id, GetUserProfileModel().Id,
                            model.ProductId);
                    }
                }

                if (removing != null)
                {
                    foreach (var item in removing)
                    {
                        ProductReselling remove = Repo.ProductResellingRepository.GetFirst(pr =>
                            pr.ProductId == model.ProductId && pr.ResellerId == item.PartnerId);

                        Repo.ProductResellingRepository.Delete(remove);
                    }
                }

                if (editing != null)
                {
                    foreach (var item in editing)
                    {
                        ProductReselling editingItem = Repo.ProductResellingRepository.GetFirst(pr =>
                            pr.ProductId == model.ProductId && pr.ResellerId == item.PartnerId);

                        editingItem.Provision = (double) item.Provision;

                        Repo.ProductResellingRepository.Update(editingItem);
                    }
                }
            }

            //assigning some flag for portal marketPlace publishing
            product.ShowOnPublicPage = model.WebsitePublishing;
            product.ShowOnMarketplace = model.MarketPlacePublishing;
            product.ProductStatus = model.IsLive;

            Repo.ProductsRepository.Update(product);
            await Repo.SaveChangesAsync();            
        }

        public async Task<Products> ProcessProduct(ProductAboutViewModel model)
        {
            Products product = await Repo.ProductsRepository.GetByIdAsync(model.ProductId);

            if (product is MasterCourses)
            {
                MasterCourses mc = product as MasterCourses;

                if (!model.WallpaperImageUrl.Contains(HttpsStringStart))
                {
                    mc.WallPaperUrl = model.WallpaperImageUrl;
                }

                mc.Tags = model.Tags;

                List<ProductsCategories> categories = mc.ProductsCategories.ToList();

                foreach (var category in model.Categories)
                {
                    if (!categories.Any(pc => pc.CategoryId == category.CategoryId))
                    {
                        Repo.ProductsCategoriesRepository.Insert(new ProductsCategories
                        {
                            CategoryId = category.CategoryId,
                            ProductId = product.Id
                        });
                    }
                }

                List<MasterCoursesDescription> descriptions = mc.MasterCoursesDescription.ToList();

                foreach (var description in model.Descriptions)
                {
                    MasterCoursesDescription editingDescription =
                        descriptions.FirstOrDefault(mcd => mcd.Language == description.LanguageId);

                    if (editingDescription != null)
                    {
                        editingDescription.LongDescription = description.Description;
                        editingDescription.Name = description.Name;

                        if (!description.CoverImageUrl.Contains(HttpsStringStart))
                        {
                            editingDescription.CoverUrl = description.CoverImageUrl;
                        }

                        editingDescription.IntroVideoUrl = description.IntroMovieUrl;
                        editingDescription.TrailerVideoUrl = description.TrailerMovieUrl;
                    }

                    Repo.MasterCoursesDescriptionRepository.Update(editingDescription);
                }                

                Repo.MasterCoursesRepository.Update(mc);
                await Repo.SaveChangesAsync();

                Repo.MasterCoursesRepository.Reload(ref mc);

                return mc;
            }
            else
            {
                Lectures lect = product as Lectures;

                lect.Tags = model.Tags;

                if (!model.Descriptions.FirstOrDefault()?.CoverImageUrl?.Contains(HttpsStringStart) ?? false)
                {
                    lect.CoverPhoto = model.Descriptions.FirstOrDefault()?.CoverImageUrl;
                }

                if (!model.WallpaperImageUrl?.Contains(HttpsStringStart) ?? false)
                {
                    lect.WallpaperUrl = model.WallpaperImageUrl;
                }

                lect.Description = model.Descriptions.FirstOrDefault()?.Description ?? string.Empty;
                lect.Name = model.Descriptions.FirstOrDefault()?.Name ?? string.Empty;

                lect.TrailerVideoUrl = model.Descriptions[0].TrailerMovieUrl;

                List<ProductsCategories> categories = lect.ProductsCategories.ToList();

                foreach (var category in categories)
                {
                    var cat = model.Categories.FirstOrDefault(c => c.CategoryId == category.CategoryId);
                    if (cat == null)
                    {
                        Repo.ProductsCategoriesRepository.Delete(category);
                    }
                }

                foreach (var category in model.Categories)
                {
                    if (!categories.Any(pc => pc.CategoryId == category.CategoryId))
                    {
                        Repo.ProductsCategoriesRepository.Insert(new ProductsCategories
                        {
                            CategoryId = category.CategoryId,
                            ProductId = product.Id
                        });
                    }
                }

                Repo.LecturesRepository.Update(lect);
                Repo.SaveChanges();

                return lect;
            }
        }
    }
}