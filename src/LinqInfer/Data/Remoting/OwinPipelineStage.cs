namespace LinqInfer.Data.Remoting
{
    public enum OwinPipelineStage
    {
        Authenticate = 0,
        Authorize = 1,
        PreHandlerExecute = 2,
        PostHandlerExecute = 3
    }
}