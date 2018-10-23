using System.Web;
using System.Web.Optimization;

namespace WEB
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            /*bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));*/

            bundles.Add(new StyleBundle("~/Content/jquery-ui-css").Include(
                      "~/Content/themes/base/jquery-ui.css"));
            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-{version}.js"));

            //added src for chosen: searchable ddl
            bundles.Add(new StyleBundle("~/chosen_v1.8.2/css")
                .Include("~/chosen_v1.8.2/chosen.css"));
            bundles.Add(new ScriptBundle("~/chosen_v1.8.2/js")
                .Include("~/chosen_v1.8.2/chosen.jquery.js"));

            //added src for bs-datepicker with locales
            bundles.Add(new StyleBundle("~/bs-datepicker/css")
                .Include("~/bootstrap-datepicker-1.6.4-dist/css/bootstrap-datepicker.css"));
            bundles.Add(new ScriptBundle("~/bs-datepicker/js")
                .Include("~/bootstrap-datepicker-1.6.4-dist/js/bootstrap-datepicker.js", "~/bootstrap-datepicker-1.6.4-dist/locales/bootstrap-datepicker.ru.min.js"));

            //No Cache Bundles
            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));
            bundles.Add(new Bundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));
        }
    }
}
