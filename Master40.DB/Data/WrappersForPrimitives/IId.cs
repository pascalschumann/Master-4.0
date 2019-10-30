namespace Master40.DB.Data.WrappersForPrimitives
{
    public interface IId
    {
        Id GetId();
        
        // replaces ToString() since debugger is unusable
        string AsString();
    }
}