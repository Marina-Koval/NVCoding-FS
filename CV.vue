<template>
    <base-layout header-title="CV" class="mCV">        
        <vimg class="mCover" :src="user.cover" :type="1"/>
        <vimg class="mAvatar pl-3" :src="user.avatar" :type="3"/>        

        <h4 class="pl-3">
            {{user.name}}
        </h4>                

        <h4 class="mTitleUnderline ma-2">Contacts</h4>
        <div class="langs pa-2">            
            <v-layout align-space-around column class="px-5">
                <v-layout justify-space-between>
                    <v-icon>alternate_email</v-icon>
                    {{user.email}}
                </v-layout>
                
                <v-layout v-if="user.phone" justify-space-between>
                    <v-icon>phone</v-icon>
                    {{user.phone}}
                </v-layout>

                <v-layout v-if="user.mobile" justify-space-between>
                    <v-icon>mobile_friendly</v-icon>
                    {{user.mobile}}
                </v-layout>

                <v-layout justify-space-between>
                    <v-icon>location_on</v-icon>
                    <v-layout align-end column>
                        <span>{{user.address}}</span>
                        <span>{{user.country}}</span>
                        <span>{{user.city}}</span>
                    </v-layout>
                </v-layout>
                
            </v-layout>
        </div>

        <h4 class="mTitleUnderline ma-2">BIO</h4>
        <div class="pa-2">
            {{user.bio}}
        </div>

        <h4 class="mTitleUnderline ma-2">Language</h4>
        <div class="langs pa-2">            
            <div>
                <v-icon>text_fields</v-icon>
                <img :src="user.lang" width="20"/>
            </div>
        </div>

    </base-layout>
</template>

<script>    
    import axios from "axios";
    import BaseLayout from "../../Comps/Layouts/BaseLayout.vue";
    import vimg from "@/vue/mobile/comps/ui/image.vue";

    let _data = null;
    async function getDataAsync() {
        return await axios.get('/NewProfile/GetMyCVMobile')
            .then((res) => { _data = res.data.Object; })
            .catch((err) => { })
    }

    export default {
        async beforeRouteEnter(to, from, next) {            
            await getDataAsync();
            next();
        },
        async beforeRouteUpdate(to, from, next) {            
            await getDataAsync();
            this.initData(_data);
            next();
        },
        name: "mCV",
        components: {     
            BaseLayout, vimg, 
        },
        data: () => ({
            user: {
                name: "",
                avatar: "",
                cover: "",
                bio: "",

                lang: "",
                email: "",

                phone: "",
                mobile: "",

                address: "",
                country: "",
                city: "",

            },
            
        }),
        created() {
            this.initData(_data);
        },
        methods: {
            initData(data) {                
                this.user.name = data.FullName;
                this.user.cover = data.Cover || "";
                this.user.avatar = data.Photo || "";
                
                this.user.bio = data.BIO;
                this.user.lang = data.ProfileLanguage.Image;
                
                this.user.email = data.Email;
                this.user.address = data.Address;
                this.user.country = data.Country;
                this.user.city = data.City;
                
                this.user.phone = data.Phone || "";
                this.user.mobile = data.Mobile;
            }
        }
    }
</script>

<style lang="less" scoped>
    .mCV {
        display: flex;
        flex-direction: column;

        .mAvatar {
            margin-top: -50px;
        }
    }
</style>