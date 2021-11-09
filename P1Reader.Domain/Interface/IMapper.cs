namespace P1Reader.Domain.Interface
{
    public interface IMapper<TSource, TTarget>
    {
        TTarget Map(
            TSource source);
    }
}
