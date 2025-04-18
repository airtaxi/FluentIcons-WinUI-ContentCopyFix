﻿using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using FluentIcons.Common;
using FluentIcons.Common.Internals;

namespace FluentIcons.Avalonia.Internals;

public abstract class GenericIcon : IconElement
{
    public static readonly StyledProperty<IconVariant> IconVariantProperty
        = AvaloniaProperty.Register<GenericIcon, IconVariant>(nameof(IconVariant));
    public static new readonly StyledProperty<double> FontSizeProperty
        = AvaloniaProperty.Register<GenericIcon, double>(nameof(FontSize), 20d, false);

    private readonly Border _border;
    private readonly Core _core;

    internal GenericIcon()
    {
        _border = new();
        _border.Bind(BackgroundProperty, this.GetBindingObservable(BackgroundProperty));
        _border.Bind(BorderBrushProperty, this.GetBindingObservable(BorderBrushProperty));
        _border.Bind(BorderThicknessProperty, this.GetBindingObservable(BorderThicknessProperty));
        _border.Bind(CornerRadiusProperty, this.GetBindingObservable(CornerRadiusProperty));
        _border.Bind(PaddingProperty, this.GetBindingObservable(PaddingProperty));
        (_border as ISetLogicalParent).SetParent(this);
        VisualChildren.Add(_border);
        LogicalChildren.Add(_border);

        _core = new();
        _core.Bind(FlowDirectionProperty, this.GetBindingObservable(FlowDirectionProperty));
        (_core as ISetLogicalParent).SetParent(this);
        VisualChildren.Add(_core);
        LogicalChildren.Add(_core);
    }

    public IconVariant IconVariant
    {
        get => GetValue(IconVariantProperty);
        set => SetValue(IconVariantProperty, value);
    }

    public new double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    protected abstract string IconText { get; }
    protected abstract Typeface IconFont { get; }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        InvalidateText();
        base.OnLoaded(e);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _core.Clear();
        base.OnUnloaded(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double fs = FontSize;
        Size size = new Size(fs, fs).Inflate(Padding).Inflate(BorderThickness);
        return new Size(
            Width.Or(
                HorizontalAlignment == HorizontalAlignment.Stretch
                    ? availableSize.Width.Or(size.Width)
                    : Math.Min(availableSize.Width, size.Width)),
            Height.Or(
                VerticalAlignment == VerticalAlignment.Stretch
                    ? availableSize.Height.Or(size.Height)
                    : Math.Min(availableSize.Height, size.Height)));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double fs = FontSize;
        Size size = new Size(fs, fs).Inflate(Padding).Inflate(BorderThickness);
        Rect rect = new(
            HorizontalAlignment switch
            {
                HorizontalAlignment.Center => (finalSize.Width - fs) / 2,
                HorizontalAlignment.Right => finalSize.Width - fs,
                _ => 0
            },
            VerticalAlignment switch
            {
                VerticalAlignment.Center => (finalSize.Height - fs) / 2,
                VerticalAlignment.Bottom => finalSize.Height - fs,
                _ => 0
            },
            HorizontalAlignment switch
            {
                HorizontalAlignment.Stretch => finalSize.Width,
                _ => size.Width,
            },
            VerticalAlignment switch
            {
                VerticalAlignment.Stretch => finalSize.Height,
                _ => size.Height,
            });
        _border.Arrange(rect);
        _core.Arrange(rect.Deflate(BorderThickness).Deflate(Padding));

        return finalSize;
    }

    protected void InvalidateText()
    {
        if (!IsLoaded) return;

        _core.InvalidateText(IconText, IconFont, FontSize, Foreground);
    }

    private sealed class Core : Control
    {
        private double _size;
        private TextLayout? _textLayout;

        public override void Render(DrawingContext context)
        {
            if (_textLayout is null)
                return;

            Rect bounds = Bounds;
            using (context.PushClip(new Rect(bounds.Size)))
            {
                IDisposable? disposable = null;
                if (FlowDirection == FlowDirection.RightToLeft)
                    disposable = context.PushTransform(new Matrix(-1, 0, 0, 1, bounds.Width, 0));
                var origin = new Point(
                    (bounds.Width - _size) / 2,
                    (bounds.Height - _size) / 2);
                _textLayout.Draw(context, origin);
                disposable?.Dispose();
            }
        }

        public void InvalidateText(string text, Typeface typeface, double fontSize, IBrush? foreground)
        {
            _size = fontSize;

            _textLayout?.Dispose();
            _textLayout = new TextLayout(
                text,
                typeface,
                fontSize,
                foreground,
                TextAlignment.Center,
                flowDirection: FlowDirection);

            InvalidateVisual();
        }

        public void Clear()
        {
            _textLayout?.Dispose();
            _textLayout = null;
        }
    }
}

public class GenericIconConverter<V, T> : TypeConverter
    where V : Enum
    where T : GenericIcon, IValue<V>, new()
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(V))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string name)
        {
            return new T { Value = (V)Enum.Parse(typeof(V), name) };
        }
        else if (value is V val)
        {
            return new T { Value = val };
        }
        return base.ConvertFrom(context, culture, value);
    }
}
