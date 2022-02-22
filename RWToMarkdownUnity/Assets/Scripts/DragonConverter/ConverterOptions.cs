public class ConverterOptions
{
    public bool FirstImageIsAlignedRight = false;
    public bool AddBordersToImages = true;

    public ConverterOptions()
    {
    }

    public ConverterOptions(bool rightAlignFirstImage = true, bool addBordersToImages = true)
    {
        FirstImageIsAlignedRight = rightAlignFirstImage;
        AddBordersToImages = addBordersToImages;
    }
}