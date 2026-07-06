namespace Nuri.Platform.Abstractions
{
    public interface IHostAdapter<TRoot>
    {
        void SetContent(TRoot root);
    }
}
