class ngScopeBase {

    getScope(): ng.IScope {
        return (<ng.IScope>(<any>this))
    }

    apply() {
        this.getScope().$apply();
    }

    toJson(src: Object) {
        var obj = this.objectFilter(src);
        var str = JSON.stringify(obj, null, "    ");
        return str;
    }

    modelJson(...ignoreFields: string[]) {

        var items = _.pairs(this);
        var obj = new Object();
        _(items).filter(i=> i[0].indexOf("$") != 0) //フレームワークのオブジェクトを除外
            .filter(i=> ignoreFields.indexOf(i[0]) == -1) //指定した名前を除外しておく
            .filter(i=> !_.isFunction(i[1]))//関数を除外
            .filter(i=> i[1] != this)　　　 //自身のオブジェクトを除外(無限ループ対策)
            .forEach(i=> obj[i[0]] = this.objectFilter(i[1]));

        var str = JSON.stringify(obj, null, "    ");
        return str;
    }

    private objectFilter(src: Object) {
        var items = _.pairs(src);

        if (items.length == 0)
            return src;

        var dst = new Object();
        _(items).filter(i=> i[0].indexOf("$") != 0) //フレームワークのオブジェクトを除外
            .filter(i=> !_.isFunction(i[1]))//関数を除外
            .filter(i=> i[1] != this)　　　 //自身のオブジェクトを除外(無限ループ対策)
            .forEach(i=> dst[i[0]] = this.objectFilter(i[1]));
        return dst;

    }

    getQueryParams(): any {
        var query = window.location.search.substring(1); // delete '?'
        if (!query) {
            return false;
        }
        return _
            .chain(query.split('&'))
            .map(function (params) {
            var p = params.split('=');
            return [p[0], decodeURIComponent(p[1])];
        })
            .object()
            .value();
    }
}
