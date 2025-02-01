using ManagedCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.VisualStudio.Jdt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.Transform
{
    /// <summary>
    /// Main class of this plugin that implement all used interfaces.
    /// </summary>
    public class Main : IPlugin, IContextMenu, IDisposable, ISettingProvider
    {
        private const string TARGET_KEY = "__target";

        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "AEBB41A80F2649878F7A00B1FF9D5CF9";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Transform";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Transform Description";

        private PluginInitContext Context { get; set; }

        private string IconPath { get; set; }

        private bool Disposed { get; set; }

        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrWhiteSpace(TransformsDirectoryPath))
            {
                return
                [
                    new Result
                    {
                        IcoPath = IconPath,
                        Title = "Transform directory path not set, please go to settings and set it."
                    }
                ];
            }
            
            var search = query.Search;
            
            // List all .json file paths without prefixing them with TransformsDirectoryPath
            var jsonFiles = Directory.GetFiles(TransformsDirectoryPath, "*.json", SearchOption.AllDirectories)
                                     .Select(x => new
                                     {
                                         FilePath = x,
                                         Title = Path.GetFileName(x)
                                     });

            return jsonFiles.Select(x => new Result
            {
                QueryTextDisplay = search,
                IcoPath = IconPath,
                Title = x.Title,
                SubTitle = "Transform file",
                Action = context => ApplyTransformation(Context, x.FilePath),
                ContextData = search
            }).ToList();
        }

        private static bool ApplyTransformation(PluginInitContext context, string transformFilePath)
        {
            try
            {
                var transformationFileJObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(transformFilePath));
                var target = transformationFileJObject.GetValue(TARGET_KEY);
                if (target is not { Type: JTokenType.String })
                {
                    throw new ApplicationException($"File: {transformFilePath} do not contains key \"{TARGET_KEY}\" ");
                }
                var targetFilePath = target.ToString();
                transformationFileJObject.Remove(TARGET_KEY);
                var transformationWithoutTarget = transformationFileJObject.ToString();

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                writer.Write(transformationWithoutTarget);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                
                var transformation = new JsonTransformation(stream);
                using var appliedTransformationStream = transformation.Apply(targetFilePath);
                
                using var targetFile = File.Open(targetFilePath, FileMode.Truncate);
                appliedTransformationStream.CopyTo(targetFile);
                context.API.ShowNotification("Transform success", $"Successfully applied transformation from {transformFilePath} on {targetFilePath}");
            }
            catch (Exception ex)
            {
                Log.Exception($"Problem with apply transformation from file {transformFilePath}" ,ex, typeof(Main));
                context.API.ShowMsg("Problem with processing", ex.Message, useMainWindowAsOwner: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult) => [];

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose()"/> that dispose additional objects and events form the plugin itself.
        /// </summary>
        /// <param name="disposing">Indicate that the plugin is disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            if (Context?.API != null)
            {
                Context.API.ThemeChanged -= OnThemeChanged;
            }

            Disposed = true;
        }

        private void UpdateIconPath(Theme theme) => IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
            ? "Images/transform.light.png"
            : "Images/transform.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);
        public Control CreateSettingPanel() => throw new NotImplementedException();
        public string TransformsDirectoryPath { get; set; }
        
        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            TransformsDirectoryPath = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(TransformsDirectoryPath))?.TextValue;
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
        [
            new()
            {
                Key = nameof(TransformsDirectoryPath),
                DisplayLabel = "Directory with transformations",
                DisplayDescription = "Specify whether to use a directory for transformation files.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = TransformsDirectoryPath,
            }
        ];
    }
}
