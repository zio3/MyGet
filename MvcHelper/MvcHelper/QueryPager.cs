using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.WebPages;
using System.Web.WebPages.Html;
using Microsoft.Internal.Web.Utils;
using System.Web.UI.WebControls;
using System.Linq.Expressions;
using System.Reflection;
using RB = Microsoft.CSharp.RuntimeBinder;
using System.Web;
using System.Web.Helpers;
using System;
using System.ComponentModel.DataAnnotations;

namespace MvcHelper
{
    public class QueryPager<T>
    {
        // jquery code for partial page update of grid components (see http://api.jquery.com/load/)
        private readonly HttpContextBase _context;
        private readonly bool _canPage;
        private readonly bool _canSort;
        private readonly string _defaultSort;
        private readonly string _pageFieldName = "page";
        private readonly string _sortDirectionFieldName = "sortdir";
        private readonly string _selectionFieldName = "row";
        private readonly string _sortFieldName = "sort";
        private readonly string _fieldNamePrefix;
        private int _pageIndex = -1;
        private bool _pageIndexSet;
        private string _sortColumn;
        private bool _sortColumnSet;
        private System.Web.Helpers.SortDirection _sortDirection;
        private bool _sortDirectionSet;
        protected IQueryable<T> _dataSource;
        protected bool _dataSourceBound;
        Func<IOrderedQueryable<T>, IQueryable<T>> _thenBy;

        /// <param name="source">Data source</param>
        /// <param name="columnNames">Data source column names. Auto-populated by default.</param>
        /// <param name="defaultSort">Default sort column.</param>
        /// <param name="rowsPerPage">Number of rows per page.</param>
        /// <param name="fieldNamePrefix">Prefix for query string fields to support multiple grids.</param>
        /// <param name="pageFieldName">Query string field name for page number.</param>
        /// <param name="selectionFieldName">Query string field name for selected row number.</param>
        /// <param name="sortFieldName">Query string field name for sort column.</param>
        /// <param name="sortDirectionFieldName">Query string field name for sort direction.</param>
#if CODE_COVERAGE 
        [ExcludeFromCodeCoverage]
#endif
        public QueryPager(
              IQueryable<T> source = null,
              string defaultSort = null,
              int rowsPerPage = 10,
              bool canPage = true,
              bool canSort = true,
              string fieldNamePrefix = null,
              string pageFieldName = null,
              string selectionFieldName = null,
              string sortFieldName = null,
              string sortDirectionFieldName = null,
              Func<IOrderedQueryable<T>, IOrderedQueryable<T>> thenBy = null
                )
            : this(new HttpContextWrapper(System.Web.HttpContext.Current), defaultSort: defaultSort, rowsPerPage: rowsPerPage, canPage: canPage,
                canSort: canSort, fieldNamePrefix: fieldNamePrefix, pageFieldName: pageFieldName,
                selectionFieldName: selectionFieldName, sortFieldName: sortFieldName, sortDirectionFieldName: sortDirectionFieldName)
        {
            if (thenBy != null)
            {
                _thenBy = thenBy;
            }

            if (source != null)
            {
                Bind(source);
            }
        }

        // NOTE: WebGrid uses an IEnumerable<dynamic> data source instead of IEnumerable<T> to avoid generics in the syntax.
        internal QueryPager(
            HttpContextBase context,
            string defaultSort = null,
            int rowsPerPage = 10,
            bool canPage = true,
            bool canSort = true,
            string fieldNamePrefix = null,
            string pageFieldName = null,
            string selectionFieldName = null,
            string sortFieldName = null,
            string sortDirectionFieldName = null)
        {

            Debug.Assert(context != null);

            if (rowsPerPage < 1)
            {
                throw new ArgumentOutOfRangeException("rowsPerPage", "rowsPerPageは1以上を指定してください");
            }

            _context = context;
            _defaultSort = defaultSort;
            this.RowsPerPage = rowsPerPage;
            _canPage = canPage;
            _canSort = canSort;

            _fieldNamePrefix = fieldNamePrefix;

            if (!String.IsNullOrEmpty(pageFieldName))
            {
                _pageFieldName = pageFieldName;
            }
            if (!String.IsNullOrEmpty(selectionFieldName))
            {
                _selectionFieldName = selectionFieldName;
            }
            if (!String.IsNullOrEmpty(sortFieldName))
            {
                _sortFieldName = sortFieldName;
            }
            if (!String.IsNullOrEmpty(sortDirectionFieldName))
            {
                _sortDirectionFieldName = sortDirectionFieldName;
            }
        }

        public string FieldNamePrefix
        {
            get
            {
                return _fieldNamePrefix ?? String.Empty;
            }
        }


        public int PageCount
        {
            get
            {
                if (!_canPage)
                {
                    return 1;
                }
                return (int)Math.Ceiling((double)TotalRowCount / RowsPerPage);
            }
        }

        public string PageFieldName
        {
            get
            {
                return FieldNamePrefix + _pageFieldName;
            }
        }

        public int PageIndex
        {
            get
            {
                if (!_canPage)
                {
                    //Default page index is 0
                    return 0;
                }
                if (!_pageIndexSet)
                {
                    int page;
                    if (!_canPage || !Int32.TryParse(QueryString[PageFieldName], out page) || (page < 1))
                    {
                        page = 1;
                    }

                    if (_dataSourceBound && page > PageCount)
                    {
                        page = PageCount;
                    }

                    _pageIndex = page - 1;
                    _pageIndexSet = true;
                }
                return _pageIndex;
            }
            set
            {
                if (!_canPage)
                {
                    throw new NotSupportedException("");
                }

                if (!_dataSourceBound)
                {
                    // Allow the user to specify arbitrary non-negative values before data binding
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture,
                        "", 0));
                    }
                    else
                    {
                        _pageIndex = value;
                        _pageIndexSet = true;
                    }
                }
                else
                {
                    // Once data bound, perform bounds check on the PageIndex. Also ensure the data source has not been materialized.
                    if ((value < 0) || (value >= PageCount))
                    {
                        throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture,
                            "", 0, (PageCount - 1)));
                    }
                    else if (value != _pageIndex)
                    {
                        _pageIndex = value;
                        _pageIndexSet = true;
                    }
                }
            }
        }

        virtual public IQueryable<T> Rows
        {
            get
            {
                EnsureDataBound();

                //TODO:ソートと可適用済みのRawを渡す
                var _rows = Sort(_dataSource, SortInfo);

                if (_thenBy != null)
                {
                    _rows = _thenBy((IOrderedQueryable<T>)_rows);
                }

                _rows = Page(_rows, PageIndex);

                return _rows;
            }
        }

        protected IQueryable<T> Sort(IQueryable<T> data, SortInfo sort)
        {
            Debug.Assert(data != null);
            var elementType = typeof(T);
            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(elementType))
            {
                // IDynamicMetaObjectProvider properties are only available through a runtime binder, so we
                // must build a custom LINQ expression for getting the dynamic property value.
                // Lambda: o => o.Property (where Property is obtained by runtime binder)
                // NOTE: lambda must not use internals otherwise this will fail in partial trust when Helpers assembly is in GAC
                var binder = RB.Binder.GetMember(RB.CSharpBinderFlags.None, sort.SortColumn, typeof(WebGrid), new RB.CSharpArgumentInfo[] {
                        RB.CSharpArgumentInfo.Create(RB.CSharpArgumentInfoFlags.None, null) });
                var param = Expression.Parameter(typeof(IDynamicMetaObjectProvider), "o");
                var getter = Expression.Dynamic(binder, typeof(object), param);
                return SortGenericExpression<object>((IQueryable<T>)data, getter, param, sort.SortDirection);
            }
            else
            {
                // The IQueryable<dynamic> data source is cast as IQueryable<object> at runtime. We must call
                // SortGenericExpression using reflection so that the LINQ expressions use the actual element type.
                // Lambda: o => o.Property[.NavigationProperty,etc]
                var param = Expression.Parameter(elementType, "o");
                Expression member = param;
                var type = elementType;
                var sorts = sort.SortColumn.Split('.');
                foreach (var name in sorts)
                {
                    PropertyInfo prop = type.GetProperty(name);
                    if (prop == null)
                    {
                        // no-op in case navigation property came from querystring (falls back to default sort)
                        if ((DefaultSort != null) && !sort.Equals(DefaultSort) && !String.IsNullOrEmpty(DefaultSort.SortColumn))
                        {
                            return Sort(data, DefaultSort);
                        }
                        return data;
                    }
                    member = Expression.Property(member, prop);
                    type = prop.PropertyType;
                }
                MethodInfo m = this.GetType().GetMethod("SortGenericExpression", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                m = m.MakeGenericMethod(member.Type);
                return (IQueryable<T>)m.Invoke(null, new object[] { data, member, param, sort.SortDirection });
            }
        }

        protected SortInfo DefaultSort
        {
            get;
            set;
        }

        protected static IQueryable<T> SortGenericExpression<TProperty>(IQueryable<T> data, Expression body,
            ParameterExpression param, System.Web.Helpers.SortDirection sortDirection)
        {

            Debug.Assert(data != null);
            Debug.Assert(body != null);
            Debug.Assert(param != null);

            // The IQueryable<dynamic> data source is cast as an IQueryable<object> at runtime.  We must cast
            // this to an IQueryable<TElement> so that the reflection done by the LINQ expressions will work.
            //IQueryable<T_> data2 = data.Cast<T_>();
            Expression<Func<T, TProperty>> lambda = Expression.Lambda<Func<T, TProperty>>(body, param);
            if (sortDirection == System.Web.Helpers.SortDirection.Descending)
            {
                return data.OrderByDescending(lambda);
            }
            else
            {
                return data.OrderBy(lambda);
            }
        }

        protected IQueryable<T> Page(IQueryable<T> sorce, int pageIndex)
        {
            if (pageIndex == -1)
                return sorce;

            return sorce.Skip(pageIndex * RowsPerPage).Take(RowsPerPage);
        }


        public int RowsPerPage
        {
            get;
            private set;
        }



        public string SortColumn
        {
            get
            {
                if (!_sortColumnSet)
                {
                    string sortColumn = QueryString[SortFieldName];
                    _sortColumn = sortColumn;
                    _sortColumnSet = true;
                }
                if (String.IsNullOrEmpty(_sortColumn))
                {
                    return _defaultSort ?? String.Empty;
                }
                return _sortColumn;
            }
            set
            {
                EnsureDataBound();
                if (!SortColumn.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    _sortColumn = value;
                }
                _sortColumnSet = true;
            }
        }

        public System.Web.Helpers.SortDirection SortDirection
        {
            get
            {
                if (!_sortDirectionSet)
                {
                    string sortDirection = QueryString[SortDirectionFieldName];
                    if (sortDirection != null)
                    {
                        if (sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase) ||
                            sortDirection.Equals("DESCENDING", StringComparison.OrdinalIgnoreCase))
                        {
                            _sortDirection = System.Web.Helpers.SortDirection.Descending;
                        }
                    }
                    _sortDirectionSet = true;
                }
                return _sortDirection;
            }
            set
            {
                if (!_dataSourceBound)
                {
                    _sortDirection = value;
                }
                else if (_sortDirection != value)
                {
                    _sortDirection = value;
                }
                _sortDirectionSet = true;
            }
        }

        protected SortInfo SortInfo
        {
            get
            {
                return new SortInfo { SortColumn = SortColumn, SortDirection = SortDirection };
            }
        }

        public string SortDirectionFieldName
        {
            get
            {
                return FieldNamePrefix + _sortDirectionFieldName;
            }
        }

        public string SortFieldName
        {
            get
            {
                return FieldNamePrefix + _sortFieldName;
            }
        }

        private int? totalRowCoiunt = null;
        public int TotalRowCount
        {
            get
            {
                if (totalRowCoiunt == null)
                {
                    totalRowCoiunt = _dataSource.Count();
                    EnsureDataBound();
                }
                return totalRowCoiunt.Value;
            }
        }

        internal static Type GetElementType(IQueryable source)
        {
            Debug.Assert(source != null, "source cannot be null");
            Type sourceType = source.GetType();

            if (source.Cast<object>().FirstOrDefault() is IDynamicMetaObjectProvider)
            {
                return typeof(IDynamicMetaObjectProvider);
            }
            else if (sourceType.IsArray)
            {
                return sourceType.GetElementType();
            }
            Type elementType = sourceType.GetInterfaces().Select(GetGenericEnumerableType).FirstOrDefault(t => t != null);

            Debug.Assert(elementType != null);
            return elementType;
        }

        private static Type GetGenericEnumerableType(Type type)
        {
            Type enumerableType = typeof(IEnumerable<>);
            if (type.IsGenericType && enumerableType.IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                return type.GetGenericArguments()[0];
            }
            return null;
        }

        private HttpContextBase HttpContext
        {
            get
            {
                return _context;
            }
        }

        private NameValueCollection QueryString
        {
            get
            {
                return HttpContext.Request.QueryString;
            }
        }

        public void Bind(IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            _dataSource = source;

            _dataSourceBound = true;
            ValidatePreDataBoundValues();

        }


        public string GetPageUrl(int pageIndex)
        {
            if (!_canPage)
            {
                throw new NotSupportedException("");
            }
            if ((pageIndex < 0) || (pageIndex >= PageCount))
            {
                throw new ArgumentOutOfRangeException("pageIndex", String.Format(CultureInfo.CurrentCulture,
                    "", 0, (PageCount - 1)));
            }

            NameValueCollection queryString = new NameValueCollection(1);
            queryString[PageFieldName] = (pageIndex + 1L).ToString(CultureInfo.CurrentCulture);
            return GetPath(queryString);
        }


        public string GetSortUrl(string column)
        {
            if (!_canSort)
            {
                throw new NotSupportedException("");
            }
            if (String.IsNullOrEmpty(column))
            {
                throw new ArgumentException("", "column");
            }

            var sort = SortColumn;
            var sortDir = System.Web.Helpers.SortDirection.Ascending;
            if (column.Equals(sort, StringComparison.OrdinalIgnoreCase))
            {
                if (SortDirection == System.Web.Helpers.SortDirection.Ascending)
                {
                    sortDir = System.Web.Helpers.SortDirection.Descending;
                }
            }

            NameValueCollection queryString = new NameValueCollection(2);
            queryString[SortFieldName] = column;
            queryString[SortDirectionFieldName] = GetSortDirectionString(sortDir);
            return GetPath(queryString, PageFieldName);
        }

        public PagerContext GetPagerContext(int numericLinksCount = 5)
        {
            var pi = new PagerContext();
            pi.TotalCount = TotalRowCount;
            pi.TotalPageCount = PageCount;
            pi.CurrentPage = PageIndex + 1;
            pi.IsPaging = pi.TotalPageCount > 1;

            if (PageIndex != -1)
            {

                if (totalRowCoiunt > 0)
                {
                    pi.FirstLink = GetPageUrl(0);
                }
                if (PageCount > 1)
                {
                    pi.LastLink = GetPageUrl(PageCount - 1);
                }

                pi.PrevLink = PageIndex == 0 ? string.Empty : GetPageUrl(PageIndex - 1);
                pi.NextLink = (PageIndex == PageCount - 1) ? string.Empty : GetPageUrl(PageIndex + 1);

            }
            int lastPage = PageCount - 1;

            List<PageLink> list = new List<PageLink>();

            if (PageCount >= 1)
            {
                int last = PageIndex + (numericLinksCount / 2);
                int first = last - numericLinksCount + 1;
                if (last > lastPage)
                {
                    first -= last - lastPage;
                    last = lastPage;
                }
                if (first < 0)
                {
                    last = Math.Min(last + (0 - first), lastPage);
                    first = 0;
                }
                for (int i = first; i <= last; i++)
                {
                    list.Add(new PageLink { LinkUrl = GetPageUrl(i), PageNumber = i + 1 });
                }
            }
            pi.PageLinks = list.ToArray();
            return pi;
        }

        /// <summary>
        /// Gets the HTML for a pager.
        /// </summary>
        /// <param name="mode">Modes for pager rendering.</param>
        /// <param name="firstText">Text for link to first page.</param>
        /// <param name="previousText">Text for link to previous page.</param>
        /// <param name="nextText">Text for link to next page.</param>
        /// <param name="lastText">Text for link to last page.</param>
        /// <param name="numericLinksCount">Number of numeric links that should display.</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Cyclomatic complexity increased due to arg checking")]
        public HelperResult Pager(
            WebGridPagerModes mode = WebGridPagerModes.NextPrevious | WebGridPagerModes.Numeric,
            string firstText = null,
            string previousText = null,
            string nextText = null,
            string lastText = null,
            int numericLinksCount = 5)
        {
            if (!_canPage)
            {
                throw new NotSupportedException("");
            }
            if (!ModeEnabled(mode, WebGridPagerModes.FirstLast) && (firstText != null))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    "", "FirstLast"), "firstText");
            }
            if (!ModeEnabled(mode, WebGridPagerModes.NextPrevious) && (previousText != null))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    "", "NextPrevious"), "previousText");
            }
            if (!ModeEnabled(mode, WebGridPagerModes.NextPrevious) && (nextText != null))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    "", "NextPrevious"), "nextText");
            }
            if (!ModeEnabled(mode, WebGridPagerModes.FirstLast) && (lastText != null))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                    "", "FirstLast"), "lastText");
            }
            if (numericLinksCount < 0)
            {
                throw new ArgumentOutOfRangeException("numericLinksCount",
                    String.Format(CultureInfo.CurrentCulture, "", 0));
            }

            int currentPage = PageIndex;
            int totalPages = PageCount;
            int lastPage = totalPages - 1;

            return new HelperResult(tw =>
            {
                if (ModeEnabled(mode, WebGridPagerModes.FirstLast) && currentPage > 1)
                {
                    if (String.IsNullOrEmpty(firstText))
                    {
                        firstText = "<<";
                    }
                    tw.Write(GetPageLinkHtml(0, firstText));
                    tw.Write(" ");
                }
                if (ModeEnabled(mode, WebGridPagerModes.NextPrevious) && currentPage > 0)
                {
                    if (String.IsNullOrEmpty(previousText))
                    {
                        previousText = "<";
                    }
                    tw.Write(GetPageLinkHtml(currentPage - 1, previousText));
                    tw.Write(" ");
                }

                if (ModeEnabled(mode, WebGridPagerModes.Numeric) && (totalPages > 1))
                {
                    int last = currentPage + (numericLinksCount / 2);
                    int first = last - numericLinksCount + 1;
                    if (last > lastPage)
                    {
                        first -= last - lastPage;
                        last = lastPage;
                    }
                    if (first < 0)
                    {
                        last = Math.Min(last + (0 - first), lastPage);
                        first = 0;
                    }
                    for (int i = first; i <= last; i++)
                    {
                        if (i == currentPage)
                        {
                            tw.Write((i + 1).ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            tw.Write(GetPageLinkHtml(i));
                        }
                        tw.Write(" ");
                    }
                }

                if (ModeEnabled(mode, WebGridPagerModes.NextPrevious) && (currentPage < lastPage))
                {
                    if (String.IsNullOrEmpty(nextText))
                    {
                        nextText = ">";
                    }
                    tw.Write(GetPageLinkHtml(currentPage + 1, nextText));
                    tw.Write(" ");
                }
                if (ModeEnabled(mode, WebGridPagerModes.FirstLast) && (currentPage < lastPage - 1))
                {
                    if (String.IsNullOrEmpty(lastText))
                    {
                        lastText = ">>";
                    }
                    tw.Write(GetPageLinkHtml(lastPage, lastText));
                }
            });
        }


        internal string GetLinkHtml(string path, string text)
        {
            TagBuilder linkTag = new TagBuilder("a");
            linkTag.MergeAttribute("href", path);
            linkTag.SetInnerText(text);
            return linkTag.ToString();
        }

        // review: make sure this is ordered
        internal string GetPath(NameValueCollection queryString, params string[] exclusions)
        {
            NameValueCollection temp = new NameValueCollection(QueryString);
            // update current query string in case values were set programmatically
            if (temp.AllKeys.Contains(PageFieldName))
            {
                temp.Set(PageFieldName, (PageIndex + 1L).ToString(CultureInfo.CurrentCulture));
            }
            if (temp.AllKeys.Contains(SortFieldName))
            {
                if (String.IsNullOrEmpty(SortColumn))
                {
                    temp.Remove(SortFieldName);
                }
                else
                {
                    temp.Set(SortFieldName, SortColumn);
                }
            }
            if (temp.AllKeys.Contains(SortDirectionFieldName))
            {
                temp.Set(SortDirectionFieldName, GetSortDirectionString(SortDirection));
            }
            // remove fields from exclusions list
            foreach (var key in exclusions)
            {
                temp.Remove(key);
            }
            // replace with new field values
            foreach (string key in queryString.Keys)
            {
                temp.Set(key, queryString[key]);
            }
            queryString = temp;

            StringBuilder sb = new StringBuilder(HttpContext.Request.Path);

            sb.Append("?");　

            var nameValues = new List<string>();

            for (int i = 0; i < queryString.Count; i++)
            {
                foreach (var item in (queryString[i].Split(',')))
	            {
                    nameValues.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(queryString.Keys[i]), HttpUtility.UrlEncode(item)));
	            }
            }
            sb.Append(string.Join("&", nameValues));
            return sb.ToString();
        }

        internal static string GetSortDirectionString(System.Web.Helpers.SortDirection sortDir)
        {
            return (sortDir == System.Web.Helpers.SortDirection.Ascending) ? "ASC" : "DESC";
        }

        protected void EnsureDataBound()
        {
            if (!_dataSourceBound)
            {
                throw new InvalidOperationException("");
            }
        }

        protected void ValidatePreDataBoundValues()
        {
            if (_canPage && _pageIndexSet && PageIndex > PageCount)
            {
                PageIndex = PageCount;
            }

            else if (_canSort && _sortColumnSet)
            {
                SortColumn = _defaultSort;
            }
        }


        private string GetPageLinkHtml(int pageIndex, string text = null)
        {
            if (String.IsNullOrEmpty(text))
            {
                text = (pageIndex + 1L).ToString(CultureInfo.CurrentCulture);
            }
            return GetLinkHtml(GetPageUrl(pageIndex), text);
        }


        // see: DataBoundControlHelper.IsBindableType
        private static bool IsBindableType(Type type)
        {
            Debug.Assert(type != null);

            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }
            return (type.IsPrimitive ||
                   type.Equals(typeof(string)) ||
                   type.Equals(typeof(DateTime)) ||
                   type.Equals(typeof(Decimal)) ||
                   type.Equals(typeof(Guid)) ||
                   type.Equals(typeof(DateTimeOffset)) ||
                   type.Equals(typeof(TimeSpan)));
        }

        private static bool ModeEnabled(WebGridPagerModes mode, WebGridPagerModes modeCheck)
        {
            return (mode & modeCheck) == modeCheck;
        }

        //IQueryable<T> typedSource;

        //public QueryPagerEF(
        //      IQueryable<T> source = null,
        //      string defaultSort = "Id",
        //      int rowsPerPage = 10,
        //      bool canPage = true,
        //      bool canSort = true,
        //      string fieldNamePrefix = null,
        //      string pageFieldName = null,
        //      string selectionFieldName = null,
        //      string sortFieldName = null,
        //      string sortDirectionFieldName = null)
        //            : base(new HttpContextWrapper(System.Web.HttpContext.Current), defaultSort: defaultSort, rowsPerPage: rowsPerPage, canPage: canPage,
        //                canSort: canSort, fieldNamePrefix: fieldNamePrefix, pageFieldName: pageFieldName,
        //                selectionFieldName: selectionFieldName, sortFieldName: sortFieldName, sortDirectionFieldName: sortDirectionFieldName)
        //        {
        //            typedSource = source;
        //            if (source != null)
        //            {
        //                Bind(source);
        //            }
        //        }

        public HelperResult GetSortLinkFor<TProperty>(Expression<Func<T, TProperty>> expression, string innerText = null)
        {
            if (string.IsNullOrEmpty(innerText))
            {
                Expression currentExpression = expression.Body;
                if (currentExpression.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)currentExpression;
                    var type = memberExpression.Member;
                    var attris = type.GetCustomAttributes(typeof(DisplayAttribute), true);
                    if (attris.Length != 0)
                    {
                        innerText = ((DisplayAttribute)attris[0]).Name;
                    }
                    else if (type.DeclaringType.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Length != 0)
                    {
                        var metaType = (MetadataTypeAttribute)(type.DeclaringType.GetCustomAttributes(typeof(MetadataTypeAttribute), true).First());

                        var mtType = metaType.MetadataClassType.GetMember(type.Name).FirstOrDefault();

                        if (mtType != null)
                        {

                            var displayAttri = mtType.GetCustomAttributes(typeof(DisplayAttribute), true);
                            if (displayAttri.Length != 0)
                            {
                                innerText = ((DisplayAttribute)displayAttri[0]).Name;
                            }
                            else
                            {
                                innerText = type.Name;
                            }
                        }
                        else
                        {
                            innerText = type.Name;
                        }
                    }
                    else
                    {
                        innerText = type.Name;
                    }
                }
                else
                {
                    throw new Exception("Not Supprted expressoin");
                }
            }

            TagBuilder linkTag = new TagBuilder("a");
            linkTag.MergeAttribute("href", GetSortUrlFor(expression));
            linkTag.SetInnerText(innerText);
            return new HelperResult(tw =>
                tw.Write(linkTag.ToString())
                );
        }

        public string GetSortUrlFor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            List<string> memberNames = new List<string>();
            Expression currentExpression = expression.Body;
            while (currentExpression.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)currentExpression;

                memberNames.Add(memberExpression.Member.Name);

                currentExpression = memberExpression.Expression;
            }

            memberNames.Reverse();
            var sortTarget = string.Join(".", memberNames.ToArray());

            return GetSortUrl(sortTarget);
        }
    }

    public class PageLink
    {
        public int PageNumber { get; set; }
        public string LinkUrl { get; set; }
    }

    public class PagerContext
    {
        public int TotalCount { get; set; }
        public bool HasFirst { get; set; }
        public int TotalPageCount { get; set; }
        public int CurrentPage { get; set; }
        public PageLink[] PageLinks { get; set; }
        public string FirstLink { get; set; }
        public string LastLink { get; set; }
        public string PrevLink { get; set; }
        public string NextLink { get; set; }
        public bool IsPaging { get; set; }
    }


    public sealed class SortInfo : IEquatable<SortInfo>
    {
        public string SortColumn { get; set; }

        public System.Web.Helpers.SortDirection SortDirection { get; set; }

        public bool Equals(SortInfo other)
        {
            return other != null
                && String.Equals(SortColumn, other.SortColumn, StringComparison.OrdinalIgnoreCase)
                && SortDirection == other.SortDirection;
        }

        public override bool Equals(object obj)
        {
            SortInfo sortInfo = obj as SortInfo;
            if (sortInfo != null)
            {
                return this.Equals(sortInfo);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return SortColumn.GetHashCode();
        }
    }

}