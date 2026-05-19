using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NexaPlay.Core.Models;

namespace NexaPlay.Presentation.Views.Pages;

public sealed class RichBlockTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? CenteredTextTemplate { get; set; }
    public DataTemplate? HeaderTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }
    public DataTemplate? VideoTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is RichBlock block)
        {
            return block.Type switch
            {
                RichBlockType.Text => TextTemplate,
                RichBlockType.CenteredText => CenteredTextTemplate,
                RichBlockType.Header => HeaderTemplate,
                RichBlockType.Image => ImageTemplate,
                RichBlockType.Video => VideoTemplate,
                _ => base.SelectTemplateCore(item)
            };
        }
        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
