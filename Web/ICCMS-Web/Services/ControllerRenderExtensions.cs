using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ICCMS_Web.Services
{
    public static class ControllerRenderExtensions
    {
        public static async Task<string> RenderViewAsync<TModel>(
            this Controller controller,
            string viewPath,
            TModel model,
            bool partial = false
        )
        {
            controller.ViewData.Model = model;
            using var writer = new StringWriter();

            var serviceProvider = controller.HttpContext.RequestServices;
            var viewEngine = (ICompositeViewEngine)
                serviceProvider.GetService(typeof(ICompositeViewEngine))!;
            var tempDataProvider = (ITempDataProvider)
                serviceProvider.GetService(typeof(ITempDataProvider))!;

            var viewResult = viewEngine.GetView(
                executingFilePath: null,
                viewPath: viewPath,
                isMainPage: !partial
            );
            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"View '{viewPath}' not found.");
            }

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                controller.ViewData,
                new TempDataDictionary(controller.HttpContext, tempDataProvider),
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}
