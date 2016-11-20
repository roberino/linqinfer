namespace LinqInfer.Maths
{
    public enum TimeResolution : long
    {
        None = 0,
        Milliseconds = 1,
        Seconds = 1000,
        Minutes = Seconds * 60,
        Hours = Minutes * 60,
        Days = Hours * 24,
        Months = Days * 28,
        Years = Days * 365
    }
}
