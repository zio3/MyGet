/// <reference path="../../Scripts/typings/underscore/underscore.d.ts" />

interface pageNavi {
    text: string;
    index: number;
    state: string;
}

class PaginationList<T> {
    CurrentItems = new Array<T>();
    pageIndex = 0;
    pageSize = 10;
    totalCount = 0;
    totalPages = 0;
    maxNaviCount = 10;
    PageNavis: pageNavi[];

    constructor(pageSize: number = 10) {
        this.pageSize = pageSize;
    }

    private genPageNavis() {
        var pageNavis = new Array<pageNavi>();

        pageNavis.push({ text: "<<", index: 0, state: (this.pageIndex != 0) ? "" : "disabled" });
        pageNavis.push({ text: "<", index: this.pageIndex - 1, state: (this.pageIndex != 0) ? "" : "disabled" });

        var naviCount = Math.min(this.totalPages, this.maxNaviCount);

        var naviStartIndex = Math.max(0, Math.min(this.totalPages - naviCount, this.pageIndex - naviCount / 2));
        var naviLastIndex = Math.min(this.totalPages, Math.max(naviCount, this.pageIndex + naviCount / 2));
        var naviNumbers = _.range(naviStartIndex, naviLastIndex);

        _.each(naviNumbers,
            (idx: number) => {
                pageNavis.push({ text: (idx + 1).toString(), index: idx, state: (this.pageIndex != idx) ? "" : "active" });
            });

        pageNavis.push({ text: ">", index: this.pageIndex + 1, state: (this.pageIndex != (this.totalPages - 1)) ? "" : "disabled" });
        pageNavis.push({ text: ">>", index: this.totalPages - 1, state: (this.pageIndex != (this.totalPages - 1)) ? "" : "disabled" });

        return pageNavis;
    }

    public ReplaceCurrentItem(items: T[], pageIndex: number, totalCount: number) {
        this.pageIndex = pageIndex;
        this.totalCount = Number(totalCount);
        this.totalPages = Math.ceil(Number(totalCount) / this.pageSize);
        this.CurrentItems = items;
        this.PageNavis = this.genPageNavis();
    }
} 