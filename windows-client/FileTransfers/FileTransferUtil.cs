using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

using System.Collections;
using System.Runtime.InteropServices;

namespace windows_client.FileTransfers
{
    public class FileTransferUtil
    {
        public static void GetCompressedJPEGImage(BitmapImage image, string fileName, int qualityIndex, out byte[] thumbnailBytes, out byte[] fileBytes)
        {
            int qFactor = FileTransferConstants.IMAGE_QUALITY_NORMAL;
            if (qualityIndex == (int)ImageQuality.Normal)
                qFactor = FileTransferConstants.IMAGE_QUALITY_NORMAL;
            else if (qualityIndex == (int)ImageQuality.Better)
                qFactor = FileTransferConstants.IMAGE_QUALITY_BETTER;
            else
                qFactor = FileTransferConstants.IMAGE_QUALITY_BEST;

            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            int thumbnailWidth, thumbnailHeight, imageWidth, imageHeight;
            imageWidth = image.PixelWidth;
            imageHeight = image.PixelHeight;

            AdjustAspectRatio(image.PixelWidth, image.PixelHeight, true, out thumbnailWidth, out thumbnailHeight);
            if (qFactor != 100)
                AdjustAspectRatio(image.PixelWidth, image.PixelHeight, false, out imageWidth, out imageHeight);

            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, thumbnailWidth, thumbnailHeight, 0, 50);
                thumbnailBytes = msSmallImage.ToArray();
            }
            if (thumbnailBytes.Length > FileTransferConstants.MAX_THUMBNAILSIZE)
            {
                using (var msSmallImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msSmallImage, thumbnailWidth, thumbnailHeight, 0, 20);
                    thumbnailBytes = msSmallImage.ToArray();
                }
            }

            if (fileName.StartsWith("{")) // this is from share picker
            {
                fileName = "PhotoChooser-" + fileName.Substring(1, fileName.Length - 2) + ".jpg";
            }
            else
                fileName = fileName.Substring(fileName.LastIndexOf("/") + 1) + ".jpg";

            using (var msLargeImage = new MemoryStream())
            {

                writeableBitmap.SaveJpeg(msLargeImage, imageWidth, imageHeight, 0, qFactor);
                fileBytes = msLargeImage.ToArray();
            }

        }

        private static void AdjustAspectRatio(int width, int height, bool isThumbnail, out int adjustedWidth, out int adjustedHeight)
        {
            int maxHeight, maxWidth;
            if (isThumbnail)
            {
                maxHeight = FileTransferConstants.ATTACHMENT_THUMBNAIL_MAX_HEIGHT;
                maxWidth = FileTransferConstants.ATTACHMENT_THUMBNAIL_MAX_WIDTH;
            }
            else
            {
                maxHeight = (FileTransferConstants.ATTACHMENT_MAX_HEIGHT < height) ? FileTransferConstants.ATTACHMENT_MAX_HEIGHT : height;
                maxWidth = (FileTransferConstants.ATTACHMENT_MAX_WIDTH < width) ? FileTransferConstants.ATTACHMENT_MAX_WIDTH : width;
            }

            if (height > width)
            {
                adjustedHeight = maxHeight;
                adjustedWidth = (width * adjustedHeight) / height;
            }
            else
            {
                adjustedWidth = maxWidth;
                adjustedHeight = (height * adjustedWidth) / width;
            }
        }


    }

}

