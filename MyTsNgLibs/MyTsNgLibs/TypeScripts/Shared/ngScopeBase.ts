class ngScopeBase {

    getScope(): ng.IScope {
            return (<ng.IScope>(<any>this))
        }
     
    apply() {
        this.getScope().$apply();
    }

    modelJson(...ignoreFields: string[]) {



        var items = _.pairs(this);
        var obj = new Object();
        _(items).filter(i=> i[0].indexOf("$") != 0) //フレームワークのオブジェクトを除外
            .filter(i=> ignoreFields.indexOf(i[0]) == -1) //指定した名前を除外しておく
            .filter(i=> !_.isFunction(i[1]))//関数を除外
            .filter(i=> i[1] != this)　　　 //自身のオブジェクトを除外(無限ループ対策)
            .forEach(i=> obj[i[0]] = i[1]);

        var str = JSON.stringify(obj, null, "    ");
        return str;
    } 
}
