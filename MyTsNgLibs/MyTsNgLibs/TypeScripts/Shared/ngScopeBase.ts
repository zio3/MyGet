/// <reference path="../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../Scripts/typings/underscore/underscore.d.ts" />


class ngScopeBase {

    getScope(): ng.IScope {
            return (<ng.IScope>(<any>this))
        }
     
    apply() {
        this.getScope().$apply();
    }

    modelJson() {
        var items = _.pairs(this);        
        var obj = new Object();
        _(items).filter(i=> i[0].indexOf("$") != 0)
                    .filter(i=> !_.isFunction(i[1]))
                    .filter(i=> i[1] != this)
                    .forEach(i=> obj[i[0]] = i[1]);

        var str = JSON.stringify(obj, null, "    ");
        return str;
    } 
}
