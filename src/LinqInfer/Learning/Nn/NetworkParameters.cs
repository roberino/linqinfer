namespace LinqInfer.Learning.Nn
{
    public class NetworkParameters
    {
        public ActivatorFunc Activator { get; set; }
        public int[] HiddenLayerCount {  get; set; }
    }
}