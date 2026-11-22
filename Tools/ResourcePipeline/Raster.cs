using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class GuxuRaster {
    public static void Process(string source, string output, int[] expected, int[] crop, int[] size, string alpha, string solid, int inset, int alphaCutoff = 0, int alphaRemapFloor = 0) {
        if (size.Length != 2 || size[0] < 1 || size[1] < 1 || size[0] > 8192 || size[1] > 8192)
            throw new InvalidDataException("Output size must be 1..8192 pixels.");
        if (alpha != "preserve" && alpha != "opaque" && alpha != "transparent")
            throw new InvalidDataException("Unknown alpha rule: " + alpha);
        if (inset < 0 || inset * 2 >= Math.Min(size[0], size[1])) throw new InvalidDataException("Invalid transparent inset.");
        if (alphaCutoff < 0 || alphaCutoff > 8) throw new InvalidDataException("alphaCutoff must be 0..8; reserve cleanup for nearly invisible edge noise.");
        if (alphaRemapFloor < 0 || alphaRemapFloor > 64) throw new InvalidDataException("alphaRemapFloor must be 0..64; use only for reviewed source matte cleanup.");
        bool passthrough = false;
        using (var result = new Bitmap(size[0], size[1], PixelFormat.Format32bppArgb)) {
            using (var graphics = Graphics.FromImage(result)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                if (!string.IsNullOrEmpty(solid)) {
                    graphics.Clear(ColorTranslator.FromHtml(solid));
                } else {
                    using (var input = new Bitmap(source)) {
                        if (expected.Length != 2 || input.Width != expected[0] || input.Height != expected[1])
                            throw new InvalidDataException("Unexpected source dimensions: " + source);
                        var rect = crop.Length == 0 ? new Rectangle(0, 0, input.Width, input.Height) :
                            crop.Length == 4 ? new Rectangle(crop[0], crop[1], crop[2], crop[3]) : throw new InvalidDataException("Crop needs x,y,width,height.");
                        if (rect.X < 0 || rect.Y < 0 || rect.Width < 1 || rect.Height < 1 || rect.Right > input.Width || rect.Bottom > input.Height)
                            throw new InvalidDataException("Crop outside source.");
                        if ((long)rect.Width * (size[1]-2*inset) != (long)rect.Height * (size[0]-2*inset))
                            throw new InvalidDataException("Resize would distort aspect ratio; specify an explicit crop.");
                        passthrough = alphaRemapFloor == 0 && alphaCutoff == 0 && inset == 0 && crop.Length == 0 && input.Width == size[0] && input.Height == size[1] && input.RawFormat.Guid == ImageFormat.Png.Guid;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        using (var attributes = new ImageAttributes()) {
                            attributes.SetWrapMode(WrapMode.TileFlipXY);
                            var destination = new Rectangle(inset, inset, size[0]-2*inset, size[1]-2*inset);
                            if (alphaRemapFloor == 0) graphics.DrawImage(input, destination, rect.X, rect.Y, rect.Width, rect.Height, GraphicsUnit.Pixel, attributes);
                            else using (var cleaned = input.Clone(rect, PixelFormat.Format32bppArgb)) {
                                // Remove the approved faint matte before interpolation, retaining smooth solid edges.
                                for (int y = 0; y < cleaned.Height; y++) for (int x = 0; x < cleaned.Width; x++) {
                                    var pixel = cleaned.GetPixel(x, y);
                                    int a = pixel.A <= alphaRemapFloor ? 0 : (pixel.A - alphaRemapFloor) * 255 / (255 - alphaRemapFloor);
                                    cleaned.SetPixel(x, y, a == 0 ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(a, pixel.R, pixel.G, pixel.B));
                                }
                                graphics.DrawImage(cleaned, destination, 0, 0, cleaned.Width, cleaned.Height, GraphicsUnit.Pixel, attributes);
                            }
                        }
                    }
                }
            }
            bool opaque = true, visible = false;
            for (int y = 0; y < result.Height; y++) for (int x = 0; x < result.Width; x++) {
                byte a = result.GetPixel(x, y).A;
                if (alphaCutoff > 0 && a <= alphaCutoff) { result.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0)); a = 0; }
                opaque &= a == 255;
                visible |= a > 0;
            }
            if (!visible || (alpha == "opaque" && !opaque) || (alpha == "transparent" && opaque))
                throw new InvalidDataException("Alpha validation failed: " + alpha);
            if (passthrough) File.Copy(source, output, true);
            else result.Save(output, ImageFormat.Png);
        }
    }

    public static void Preview(string source, string foreground, string background, string output) {
        using (var input = new Bitmap(source))
        using (var front = new Bitmap(foreground))
        using (var back = new Bitmap(background))
        using (var result = new Bitmap(900, 330))
        using (var graphics = Graphics.FromImage(result))
        using (var font = new Font("Segoe UI", 11)) {
            graphics.Clear(Color.FromArgb(24, 29, 33));
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int[] sizes = {48, 72, 96, 192};
            int x = 24;
            foreach (int size in sizes) {
                graphics.DrawImage(input, new Rectangle(x, 50, size, size));
                graphics.DrawString(size + " px", font, Brushes.White, x, 24);
                x += size + 24;
            }
            using (var mask = new GraphicsPath()) {
                mask.AddEllipse(555, 50, 140, 140);
                graphics.SetClip(mask);
                // Android's visible viewport is 72 of the 108 dp layer, before shape masking.
                graphics.DrawImage(back, new Rectangle(555, 50, 140, 140));
                float inset = front.Width / 6f;
                graphics.DrawImage(front, new Rectangle(555, 50, 140, 140), inset, inset, front.Width - 2 * inset, front.Height - 2 * inset, GraphicsUnit.Pixel);
                graphics.ResetClip();
            }
            graphics.DrawString("Adaptive / circle", font, Brushes.White, 555, 24);
            graphics.DrawString("Full-bleed original / Android adaptive center crop", font, Brushes.White, 24, 280);
            result.Save(output, ImageFormat.Png);
        }
    }
}
