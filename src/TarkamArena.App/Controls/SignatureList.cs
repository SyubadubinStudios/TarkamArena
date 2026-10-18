using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;

namespace TarkamArena.App.Controls;

/// <summary>
/// The move list for a fighter's three signature moves: a chip in the move's colour with the
/// motion, the name and kind, the Jurus cost as diamonds, and the description.
/// </summary>
public static class SignatureList
{
    public static StackPanel Build(CharacterDefinition c, bool compact = false)
    {
        var list = new StackPanel { Spacing = compact ? 8 : 12 };
        foreach (var move in SignatureCatalog.For(c)) list.Children.Add(Row(move, compact));
        return list;
    }

    private static Control Row(SignatureMove move, bool compact)
    {
        var chip = new Border
        {
            Background = Ui.Brush(move.Theme.Primary),
            BorderBrush = Ui.Arang,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10, 4),
            MinWidth = compact ? 120 : 136,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new SkewTransform(-8, 0),
            Child = new TextBlock
            {
                Text = move.Input,
                FontFamily = Ui.Mono,
                FontSize = compact ? 15 : 16,
                FontWeight = FontWeight.Bold,
                Foreground = Luminance(move.Theme.Primary) > 0.55 ? Ui.Arang : Ui.Kapur,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };

        var diamonds = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        for (int i = 0; i <= move.Slot; i++)
            diamonds.Children.Add(new Border { Width = 9, Height = 9, Background = Ui.Spanduk, BorderBrush = Ui.Arang, BorderThickness = new Thickness(1), RenderTransform = new RotateTransform(45) });

        var title = Ui.H(10,
            new TextBlock { Text = move.Name.ToString().ToUpperInvariant(), FontFamily = Ui.Display, FontSize = compact ? 19 : 22, Foreground = Ui.Kapur },
            diamonds,
            Ui.Text($"{SignatureCatalog.KindName(move.Kind)} · {move.Cost}", "eyebrow").With(t => { t.FontSize = 12; t.VerticalAlignment = VerticalAlignment.Center; t.Foreground = Ui.Brush(move.Theme.Secondary); }));
        var text = compact ? (Control)title : Ui.V(2, title, Ui.Wrap(move.Description.ToString(), "body").With(t => { t.FontSize = 14; t.Foreground = Ui.KapurMuted; }));
        return Ui.Columns("Auto,*", chip, text.With(t => t.Margin = new Thickness(14, 0, 0, 0)));
    }

    private static double Luminance(uint rgb) =>
        (0.299 * ((rgb >> 16) & 0xFF) + 0.587 * ((rgb >> 8) & 0xFF) + 0.114 * (rgb & 0xFF)) / 255.0;
}
