namespace LuxOnAir
{
    public static class ColorHelper
    {
        /// <summary>
        /// Convert an integer argb value to a brush
        /// </summary>
        /// <param name="argbColor">argb value to convert</param>
        /// <returns></returns>
        public static System.Windows.Media.Brush ToBrush(this int argbColor)
        {
            var color = System.Drawing.Color.FromArgb(argbColor);
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
