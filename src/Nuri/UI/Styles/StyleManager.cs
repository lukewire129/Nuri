using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Nuri.Constants;
using Nuri.UI.Dsl;
using Nuri.UI.Values;
using YamlDotNet.RepresentationModel;

namespace Nuri.UI.Styles
{
    public sealed class StyleConfiguration : IDisposable
    {
        private readonly List<StyleSource> _sources = new List<StyleSource>();


        internal IReadOnlyList<StyleSource> Sources => _sources;

        public StyleConfiguration AddEmbeddedYaml(string yaml, string name = "embedded")
        {
            if (yaml == null)
                throw new ArgumentNullException(nameof(yaml));

            _sources.Add(StyleSource.EmbeddedYaml(name, yaml));
            return this;
        }

        public StyleConfiguration AddEmbeddedResource(Assembly assembly, string resourceName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Resource name cannot be empty.", nameof(resourceName));

            _sources.Add(StyleSource.EmbeddedResource(assembly, resourceName));
            return this;
        }

        public StyleConfiguration AddFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Style file path cannot be empty.", nameof(path));

            _sources.Add(StyleSource.File(path));
            return this;
        }

        public void Dispose()
        {
            StyleManager.Reset(this);
        }
    }

    public sealed class StyleLoadError
    {
        internal StyleLoadError(string source, string message, int line, int column)
        {
            Source = source;
            Message = message;
            Line = line;
            Column = column;
        }

        public string Source { get; }

        public string Message { get; }

        public int Line { get; }

        public int Column { get; }

        public override string ToString()
        {
            var location = Line > 0 ? $" ({Line},{Column})" : string.Empty;
            return $"[Nuri.Style] Failed to load {Source}{location}: {Message}";
        }
    }

    public static class StyleManager
    {
        private static readonly object Gate = new object();
        private static StyleConfiguration? _configuration;
        private static StyleRegistry _registry = StyleRegistry.Empty;


        public static event EventHandler<StyleLoadError>? LoadFailed;

        public static StyleRegistry Registry => Volatile.Read(ref _registry);

        public static void Configure(StyleConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            lock (Gate)
            {
                _configuration = configuration;
                var fallback = BuildRegistry(configuration, embeddedOnly: true);
                if (!TryBuildRegistry(configuration, fallback, out var registry))
                    registry = fallback;

                Install(registry);
            }
        }

        public static void Reset()
        {
            lock (Gate)
            {
                _configuration = null;
                Install(StyleRegistry.Empty);
            }
        }

        internal static void Reset(StyleConfiguration configuration)
        {
            lock (Gate)
            {
                if (ReferenceEquals(_configuration, configuration))
                    Reset();
            }
        }

        public static void Apply(IElement element)
        {
            if (string.IsNullOrWhiteSpace(element.StyleName))
                return;

            if (!Registry.TryGet(element.StyleName, out var style))
            {
                foreach (var propertyName in element.AppliedStyleProperties)
                    element.Properties.Remove(propertyName);
                element.AppliedStyleProperties.Clear();
                return;
            }

            var removedProperties = new List<string>();
            foreach (var propertyName in element.AppliedStyleProperties)
            {
                if (!style.Properties.ContainsKey(propertyName))
                    removedProperties.Add(propertyName);
            }
            foreach (var propertyName in removedProperties)
            {
                element.Properties.Remove(propertyName);
                element.AppliedStyleProperties.Remove(propertyName);
            }

            foreach (var property in style.Properties)
            {
                if (element.AppliedStyleProperties.Contains(property.Key)
                    || !element.Properties.ContainsKey(property.Key))
                {
                    element.Properties[property.Key] = property.Value;
                    element.AppliedStyleProperties.Add(property.Key);
                }
            }
        }

        private static bool TryBuildRegistry(StyleConfiguration configuration, StyleRegistry fallback, out StyleRegistry registry)
        {
            try
            {
                registry = BuildRegistry(configuration, embeddedOnly: false);
                return true;
            }
            catch (StyleFormatException exception)
            {
                registry = fallback;
                Report(exception);
                return false;
            }
            catch (IOException exception)
            {
                registry = fallback;
                Report(new StyleFormatException("styles", exception.Message, 0, 0));
                return false;
            }
            catch (Exception exception)
            {
                registry = fallback;
                Report(new StyleFormatException("styles", exception.Message, 0, 0));
                return false;
            }
        }

        private static StyleRegistry BuildRegistry(StyleConfiguration configuration, bool embeddedOnly)
        {
            var sheets = new List<RawStyleSheet>();
            foreach (var source in configuration.Sources)
            {
                if (embeddedOnly && !source.IsEmbedded)
                    continue;
                if (!source.TryRead(out var yaml))
                    continue;

                sheets.Add(RawStyleSheet.Parse(source.Name, yaml));
            }

            return StyleRegistry.Create(sheets);
        }


        private static void Install(StyleRegistry registry)
        {
            Volatile.Write(ref _registry, registry);
        }

        private static void Report(StyleFormatException exception)
        {
            var error = new StyleLoadError(exception.StyleSource, exception.Message, exception.Line, exception.Column);
            System.Diagnostics.Debug.WriteLine(error.ToString());
            System.Diagnostics.Trace.TraceError(error.ToString());
            LoadFailed?.Invoke(null, error);
        }
    }

    public sealed class StyleRegistry
    {
        private readonly IReadOnlyDictionary<string, ComputedStyle> _styles;

        private StyleRegistry(IReadOnlyDictionary<string, ComputedStyle> styles)
        {
            _styles = styles;
        }

        internal static StyleRegistry Empty { get; } = new StyleRegistry(new Dictionary<string, ComputedStyle>(StringComparer.Ordinal));

        public bool TryGet(string styleName, out ComputedStyle style)
        {
            return _styles.TryGetValue(styleName, out style!);
        }

        internal static StyleRegistry Create(IEnumerable<RawStyleSheet> sheets)
        {
            var styles = new Dictionary<string, Dictionary<string, RawValue>>(StringComparer.Ordinal);
            var tokens = new Dictionary<string, RawValue>(StringComparer.Ordinal);
            foreach (var sheet in sheets)
                sheet.MergeInto(styles, tokens);

            var resolved = new Dictionary<string, ComputedStyle>(StringComparer.Ordinal);
            var resolving = new HashSet<string>(StringComparer.Ordinal);
            foreach (var styleName in styles.Keys)
                ResolveStyle(styleName, styles, tokens, resolved, resolving);

            return new StyleRegistry(resolved);
        }

        private static ComputedStyle ResolveStyle(
            string styleName,
            IReadOnlyDictionary<string, Dictionary<string, RawValue>> styles,
            IReadOnlyDictionary<string, RawValue> tokens,
            IDictionary<string, ComputedStyle> resolved,
            ISet<string> resolving)
        {
            if (resolved.TryGetValue(styleName, out var existing))
                return existing;
            if (!styles.TryGetValue(styleName, out var raw))
                throw new StyleFormatException("styles", $"Style '{styleName}' does not exist.", 0, 0);
            if (!resolving.Add(styleName))
                throw new StyleFormatException(raw["extends"].Source, $"Style inheritance cycle includes '{styleName}'.", raw["extends"].Line, raw["extends"].Column);

            var properties = new Dictionary<string, object>(StringComparer.Ordinal);
            if (raw.TryGetValue("extends", out var extends))
            {
                var baseName = extends.GetScalar();
                var baseStyle = ResolveStyle(baseName, styles, tokens, resolved, resolving);
                foreach (var property in baseStyle.Properties)
                    properties[property.Key] = property.Value;
            }

            foreach (var pair in raw)
            {
                if (pair.Key == "extends")
                    continue;

                try
                {
                    var propertyKey = StylePropertyConverter.Convert(pair.Key, pair.Value, tokens);
                    properties[propertyKey] = StylePropertyConverter.ConvertValue(pair.Key, pair.Value, tokens);
                }
                catch (StyleFormatException exception)
                {
                    throw new StyleFormatException(
                        exception.StyleSource,
                        $"Style '{styleName}'. {exception.Message}",
                        exception.Line,
                        exception.Column);
                }
            }

            resolving.Remove(styleName);
            var style = new ComputedStyle(properties);
            resolved[styleName] = style;
            return style;
        }
    }

    public sealed class ComputedStyle
    {
        internal ComputedStyle(IReadOnlyDictionary<string, object> properties)
        {
            Properties = properties;
        }

        public IReadOnlyDictionary<string, object> Properties { get; }
    }

    internal sealed class StyleSource
    {
        private StyleSource(string name, string? yaml, Assembly? assembly, string? resourceName, string? path)
        {
            Name = name;
            Yaml = yaml;
            Assembly = assembly;
            ResourceName = resourceName;
            Path = path;
        }

        public string Name { get; }
        public string? Yaml { get; }
        public Assembly? Assembly { get; }
        public string? ResourceName { get; }
        public string? Path { get; }
        public bool IsEmbedded => Path == null;

        public static StyleSource EmbeddedYaml(string name, string yaml) => new StyleSource(name, yaml, null, null, null);
        public static StyleSource EmbeddedResource(Assembly assembly, string resourceName) => new StyleSource(resourceName, null, assembly, resourceName, null);
        public static StyleSource File(string path) => new StyleSource(path, null, null, null, System.IO.Path.GetFullPath(path));

        public bool TryRead(out string yaml)
        {
            if (Path != null)
            {
                if (!System.IO.File.Exists(Path))
                {
                    yaml = string.Empty;
                    return false;
                }
                yaml = System.IO.File.ReadAllText(Path);
                return true;
            }

            if (Yaml != null)
            {
                yaml = Yaml;
                return true;
            }

            using (var stream = Assembly!.GetManifestResourceStream(ResourceName!))
            {
                if (stream == null)
                    throw new StyleFormatException(Name, "Embedded style resource was not found.", 0, 0);
                using (var reader = new StreamReader(stream))
                    yaml = reader.ReadToEnd();
                return true;
            }
        }
    }

    internal sealed class RawStyleSheet
    {
        private readonly Dictionary<string, Dictionary<string, RawValue>> _styles;
        private readonly Dictionary<string, RawValue> _tokens;

        private RawStyleSheet(Dictionary<string, Dictionary<string, RawValue>> styles, Dictionary<string, RawValue> tokens)
        {
            _styles = styles;
            _tokens = tokens;
        }

        public static RawStyleSheet Parse(string source, string yaml)
        {
            var stream = new YamlStream();
            try
            {
                stream.Load(new StringReader(yaml));
            }
            catch (Exception exception)
            {
                throw new StyleFormatException(source, exception.Message, 0, 0);
            }

            if (stream.Documents.Count == 0 || !(stream.Documents[0].RootNode is YamlMappingNode root))
                throw new StyleFormatException(source, "A style document must contain a root mapping.", 0, 0);

            var styles = new Dictionary<string, Dictionary<string, RawValue>>(StringComparer.Ordinal);
            var tokens = new Dictionary<string, RawValue>(StringComparer.Ordinal);
            foreach (var entry in root.Children)
            {
                var name = GetKey(source, entry.Key);
                if (name == "styles")
                    ParseStyles(source, entry.Value, styles);
                else if (name == "theme")
                    FlattenTheme(source, entry.Value, string.Empty, tokens);
                else
                    throw Error(source, entry.Key, "Only 'styles' and 'theme' are allowed at the document root.");
            }

            return new RawStyleSheet(styles, tokens);
        }

        public void MergeInto(IDictionary<string, Dictionary<string, RawValue>> styles, IDictionary<string, RawValue> tokens)
        {
            foreach (var token in _tokens)
                tokens[token.Key] = token.Value;
            foreach (var style in _styles)
            {
                if (!styles.TryGetValue(style.Key, out var target))
                {
                    target = new Dictionary<string, RawValue>(StringComparer.Ordinal);
                    styles.Add(style.Key, target);
                }
                foreach (var property in style.Value)
                    target[property.Key] = property.Value;
            }
        }

        private static void ParseStyles(string source, YamlNode node, IDictionary<string, Dictionary<string, RawValue>> styles)
        {
            var mapping = RequireMapping(source, node, "'styles' must be a mapping.");
            foreach (var entry in mapping.Children)
            {
                var styleName = GetKey(source, entry.Key);
                var styleProperties = RequireMapping(source, entry.Value, $"Style '{styleName}' must be a mapping.");
                var target = new Dictionary<string, RawValue>(StringComparer.Ordinal);
                foreach (var property in styleProperties.Children)
                    target.Add(GetKey(source, property.Key), new RawValue(source, property.Value));
                styles.Add(styleName, target);
            }
        }

        private static void FlattenTheme(string source, YamlNode node, string prefix, IDictionary<string, RawValue> tokens)
        {
            var mapping = RequireMapping(source, node, "'theme' must be a mapping.");
            foreach (var entry in mapping.Children)
            {
                var key = string.IsNullOrEmpty(prefix) ? GetKey(source, entry.Key) : prefix + "." + GetKey(source, entry.Key);
                if (entry.Value is YamlMappingNode)
                    FlattenTheme(source, entry.Value, key, tokens);
                else
                    tokens.Add(key, new RawValue(source, entry.Value));
            }
        }

        private static string GetKey(string source, YamlNode node)
        {
            if (node is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
                return scalar.Value!;
            throw Error(source, node, "Mapping keys must be non-empty strings.");
        }

        private static YamlMappingNode RequireMapping(string source, YamlNode node, string message)
        {
            if (node is YamlMappingNode mapping)
                return mapping;
            throw Error(source, node, message);
        }

        private static StyleFormatException Error(string source, YamlNode node, string message)
        {
            return new StyleFormatException(source, message, checked((int)node.Start.Line + 1), checked((int)node.Start.Column + 1));
        }
    }

    internal sealed class RawValue
    {
        public RawValue(string source, YamlNode node)
        {
            Source = source;
            Node = node;
            Line = checked((int)node.Start.Line + 1);
            Column = checked((int)node.Start.Column + 1);
        }

        public string Source { get; }
        public YamlNode Node { get; }
        public int Line { get; }
        public int Column { get; }

        public string GetScalar()
        {
            if (Node is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
                return scalar.Value!;
            throw new StyleFormatException(Source, "Expected a non-empty string.", Line, Column);
        }

        public string Describe()
        {
            if (Node is YamlScalarNode scalar)
                return scalar.Value ?? string.Empty;
            if (Node is YamlSequenceNode)
                return "[sequence]";
            if (Node is YamlMappingNode)
                return "{mapping}";
            return Node.GetType().Name;
        }
    }

    internal static class StylePropertyConverter
    {
        private static readonly Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["width"] = PropertyKeys.Width,
            ["height"] = PropertyKeys.Height,
            ["min-width"] = PropertyKeys.MinWidth,
            ["min-height"] = PropertyKeys.MinHeight,
            ["max-width"] = PropertyKeys.MaxWidth,
            ["max-height"] = PropertyKeys.MaxHeight,
            ["padding"] = "Padding",
            ["margin"] = "Margin",
            ["gap"] = PropertyKeys.Spacing,
            ["background"] = PropertyKeys.Background,
            ["foreground"] = PropertyKeys.Foreground,
            ["radius"] = "CornerRadius",
            ["border-width"] = "BorderThickness",
            ["border-color"] = "BorderBrush",
            ["font-size"] = "FontSize",
            ["font-weight"] = "FontWeight",
            ["opacity"] = "Opacity"
        };

        public static string Convert(string property, RawValue value, IReadOnlyDictionary<string, RawValue> tokens)
        {
            if (Properties.TryGetValue(property, out var key))
                return key;
            throw new StyleFormatException(value.Source, $"Style property '{property}' is not supported.", value.Line, value.Column);
        }

        public static object ConvertValue(string property, RawValue raw, IReadOnlyDictionary<string, RawValue> tokens)
        {
            var value = ResolveToken(raw, tokens, new HashSet<string>(StringComparer.Ordinal));
            switch (property)
            {
                case "width":
                case "height":
                case "min-width":
                case "min-height":
                case "max-width":
                case "max-height":
                case "gap":
                case "font-size":
                    return Number(value, property, nonNegative: true);
                case "opacity":
                    var opacity = Number(value, property, nonNegative: true);
                    if (opacity > 1)
                        throw Invalid(value, property, "a number from 0 through 1");
                    return opacity;
                case "padding":
                case "margin":
                case "border-width":
                    return Thickness(value, property);
                case "radius":
                    return CornerRadiusValue.Uniform(Number(value, property, nonNegative: true));
                case "background":
                case "foreground":
                case "border-color":
                    return new BrushValue.Solid(Color(value, property));
                case "font-weight":
                    var weight = Number(value, property, nonNegative: true);
                    if (weight != Math.Truncate(weight))
                        throw Invalid(value, property, "an integer font weight");
                    return new FontWeightValue((int)weight);
                default:
                    throw new InvalidOperationException($"No converter exists for '{property}'.");
            }
        }

        private static RawValue ResolveToken(RawValue value, IReadOnlyDictionary<string, RawValue> tokens, ISet<string> resolving)
        {
            if (!(value.Node is YamlScalarNode scalar) || string.IsNullOrEmpty(scalar.Value) || scalar.Value![0] != '$')
                return value;

            var name = scalar.Value.Substring(1);
            if (!tokens.TryGetValue(name, out var token))
                throw new StyleFormatException(value.Source, $"Token '${name}' does not exist.", value.Line, value.Column);
            if (!resolving.Add(name))
                throw new StyleFormatException(value.Source, $"Token reference cycle includes '${name}'.", value.Line, value.Column);
            var resolved = ResolveToken(token, tokens, resolving);
            resolving.Remove(name);
            return resolved;
        }

        private static double Number(RawValue value, string property, bool nonNegative)
        {
            if (!(value.Node is YamlScalarNode scalar) || !double.TryParse(scalar.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) || double.IsNaN(number) || double.IsInfinity(number) || (nonNegative && number < 0))
                throw Invalid(value, property, nonNegative ? "a finite non-negative number" : "a finite number");
            return number;
        }

        private static ColorValue Color(RawValue value, string property)
        {
            if (!(value.Node is YamlScalarNode scalar) || string.IsNullOrWhiteSpace(scalar.Value))
                throw Invalid(value, property, "a color string such as '#5B8CFF'");
            try
            {
                return ColorValue.FromHex(scalar.Value!);
            }
            catch (Exception)
            {
                throw Invalid(value, property, "a color string such as '#5B8CFF'");
            }
        }

        private static ThicknessValue Thickness(RawValue value, string property)
        {
            if (value.Node is YamlScalarNode)
                return ThicknessValue.Uniform(Number(value, property, nonNegative: true));
            if (value.Node is YamlSequenceNode sequence && sequence.Children.Count == 2)
            {
                var vertical = Number(new RawValue(value.Source, sequence.Children[0]), property, nonNegative: true);
                var horizontal = Number(new RawValue(value.Source, sequence.Children[1]), property, nonNegative: true);
                return new ThicknessValue(horizontal, vertical, horizontal, vertical);
            }
            if (value.Node is YamlMappingNode mapping)
            {
                return new ThicknessValue(
                    Side(mapping, value.Source, "left", property),
                    Side(mapping, value.Source, "top", property),
                    Side(mapping, value.Source, "right", property),
                    Side(mapping, value.Source, "bottom", property));
            }
            throw Invalid(value, property, "a number, [vertical, horizontal], or top/right/bottom/left mapping");
        }

        private static double Side(YamlMappingNode mapping, string source, string name, string property)
        {
            foreach (var pair in mapping.Children)
            {
                if (pair.Key is YamlScalarNode key && key.Value == name)
                    return Number(new RawValue(source, pair.Value), property, nonNegative: true);
            }
            throw new StyleFormatException(source, $"Style property '{property}' requires '{name}'.", checked((int)mapping.Start.Line + 1), checked((int)mapping.Start.Column + 1));
        }

        private static StyleFormatException Invalid(RawValue value, string property, string expected)
        {
            return new StyleFormatException(value.Source, $"Style property '{property}' has invalid value '{value.Describe()}'. Expected {expected}.", value.Line, value.Column);
        }
    }

    internal sealed class StyleFormatException : Exception
    {
        public StyleFormatException(string source, string message, int line, int column)
            : base(message)
        {
            StyleSource = source;
            Line = line;
            Column = column;
        }

        public string StyleSource { get; }
        public int Line { get; }
        public int Column { get; }
    }
}
