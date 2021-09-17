using EPiServer.Cms.TinyMce.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.Initialization
{
    [ModuleDependency(typeof(TinyMceInitialization))]
    public class ExtendedTinyMceInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.Configure<TinyMceConfiguration>(config =>
            {
                config.Default()
                    .ContentCss("/static/css/wysiwyg.css")
                    .AddPlugin("media wordcount anchor code importcss")
                    .Toolbar("styleselect formatselect | epi-personalized-content epi-link anchor numlist bullist indent outdent bold italic underline alignleft aligncenter alignright | image epi-image-editor media code | epi-dnd-processor | removeformat | fullscreen")
                    .AddSetting("image_caption", true)
                    .AddSetting("image_advtab", true);
            });
        }
    }
}