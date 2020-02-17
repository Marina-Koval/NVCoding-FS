<template>
    <main v-cloak class="vue_page">
        <div class="invited__page">

            <vaside size="50" :remain="aside.count" :scroll="selectedIndex" 
                    class="invited__aside"
                    @onclick="onRowClick"
                    @onbottom="filtering">

                <div slot="filters">

                    <div class="flex flex_end pd5 filters__actions">
                        <i class="ion-person icon_filter filters__icon" @click="onRedirect"></i>
                    </div>

                    <div class="flex flex_center pd10">
                        <i class="ion-ios-search filters__icon"></i>
                        <input class="filters__search input_underline" :placeholder="localization.search" v-model="onInput" />
                    </div>                   
                </div>

                <div slot="total" class="aside__total pd5" v-cloak>
                    {{aside.count}} {{localization.results}}
                </div>

                <div slot="row" class="aside__row" v-for="(item, index) of aside.data" :key="index"
                     :class="{'aside__row_active': item.Selected}"
                     @click="onRowClick(item)">
                    
                    <span class="aside__row__title">{{ item.Value }}</span>
                </div>
            </vaside>

            <div class="invited__body">
                <router-view @onUpdateFilters="searching"></router-view>
            </div>
        </div>
    </main>
</template>

<script>
import axios from "axios";

import _vselect from "../../../vue/Html/Select.vue";
import _vimage from "../../../vue/Html/Image.vue";

import _vaside from "../../../vue/Components/Asides/ScrollAside.vue";

    export default {
        name: "ManagementInviters",
        components: {
            vaside: _vaside,
            vimg: _vimage
        },
        data: () => ({
            localization: window.localization,

            //aside
            load: false,
            aside: {
                data: [],
                count: 0
            },

            searchQuery: "",
            pagination: 0,

            //glob
            currentRoute: "invited",
            selectedItem: {},
            selectedIndex: 0
        }),
        created() {
            this.init();
            window.addEventListener("keydown", this.onPressKey, false);
        },

        destroy() {
            window.removeEventListener("keydown", this.onPressKey);
        },

        computed: {
            onInput: {
                get() {
                    return this.searchQuery;
                },
                set(value) {
                    this.searchQuery = value;
                    this.searching();
                }
            }
        },
        methods: {
            init() {
                this.filtering();
            },

            searching: _.debounce(function () {
                this.pagination = 0;
                this.nopagination = false;
                this.aside.data = [];
                this.aside.count = 0;
                this.filtering();
            }, 500),

            getFilterData() {
                return {
                    Pagination: this.pagination,
                    Search: this.searchQuery
                };
            },

            filtering() {
                if (!this.nopagination) {
                    this.nopagination = true;
                    axios
                        .get("/Management/FilteringManagerInviters", {
                            params: this.getFilterData()
                        })
                        .then(response => {
                            if (!_.isUndefined(response.data.Object)) {
                                this.onSuccessFiltering(response.data.Object);
                            }
                        })
                        .catch(() => { });
                }
            },

            onSuccessFiltering(data) {
                if (data.Count) {
                    let newQuery = this.aside.data.length == 0;

                    this.aside.data = this.aside.data.concat(this.mapItems(data.List));
                    this.aside.count = data.Count;
                    this.pagination += this.aside.data.length;
                    this.nopagination = data.List.length === 0;

                    if (newQuery) {
                        this.selectNewItem(this.aside.data[0]);
                        this.$router.push({
                            name: this.currentRoute,
                            params: { id: this.selectedItem.Id }
                        });
                    }
                } else {
                    //clear page
                    this.$router.push({ path: "/noroute" });
                }
            },

            mapItems(newItems) {
                return _.map(newItems, s => {
                    return {
                        Id: s.Id,
                        Value: s.Title,
                        //CompanyId: s.CompanyId,
                        Selected: false
                    };
                });
            },

            onRowClick(item) {
                if (this.selectedItem.Id != item.Id) {
                    this.selectNewItem(item);
                    this.$router.push({
                        name: this.currentRoute,
                        params: { id: this.selectedItem.Id }
                    });
                }
            },

            selectNewItem(item) {
                this.selectedItem.Selected = false;
                this.selectedItem = item;
                this.selectedItem.Selected = true;
            },

            onPressKey(e) {
                e = e || window.event;
                if ([37, 38, 39, 40].indexOf(e.keyCode) > -1) {
                    e.preventDefault();
                }
                if (e.keyCode == 38) this.onKeyUp();
                else if (e.keyCode == 40) this.onKeyDown();
            },

            onKeyDown() {
                let index = _.findIndex(this.aside.data, e => {
                    return e.Id == this.selectedItem.Id;
                });
                if (index < this.aside.count - 1) {
                    this.selectNewItem(this.aside.data[++index]);
                    this.$router.push({
                        name: this.currentRoute,
                        params: { id: this.selectedItem.Id }
                    });
                    this.selectedIndex = index;
                }
            },

            onKeyUp() {
                let index = _.findIndex(this.aside.data, e => {
                    return e.Id == this.selectedItem.Id;
                });
                if (index > 0) {
                    this.selectNewItem(this.aside.data[--index]);
                    this.$router.push({
                        name: this.currentRoute,
                        params: { id: this.selectedItem.Id }
                    });
                    this.selectedIndex = index;
                }
            },

            onRedirect() {
                window.location.href = "/Management/Organising";
            }
        }
    };
</script>

<style scoped>
.invited__body {
    margin-left: 300px;
}

.invited__aside {
    height: calc(100% - 70px);
}

.aside__row {
    display: flex;
    align-items: center;
    height: 50px;
    padding-left: 20px;
    cursor: pointer;
}

.aside__row_active,
.aside__row:hover {
    background-color: grey;
}

.aside__total {
    text-align: right;
    padding-right: 20px;
}

.aside__row__img {
    display: flex;
    border-radius: 50%;
    width: 40px;
    height: 40px;
    margin: 0 20px;
}

.aside__row__title {
    display: flex;
    justify-content: center;
    align-items: center;
}

.filters__icon {
    font-size: 24px;
    padding: 0 5px;
}

.filters__search {
    background-color: lightgray;
    padding: 0 10px;
}
</style>