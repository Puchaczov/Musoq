namespace FQL.Schema.Csv
{
    public static class SizeConverter
    {
        public static long ToGigabytes(long size)
        {
            return ToMegabytes(size) / 1024;
        }

        public static long ToMegabytes(long size)
        {
            return ToKilobytes(size) / 1024;
        }

        public static long ToKilobytes(long size)
        {
            return size / 1024;
        }
    }
}