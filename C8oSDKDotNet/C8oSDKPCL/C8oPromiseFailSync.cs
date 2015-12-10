namespace Convertigo.SDK
{
    public interface C8oPromiseFailSync<T> : C8oPromiseSync<T>
    {
        C8oPromiseSync<T> Fail(C8oOnFail c8oOnFail);
        C8oPromiseSync<T> FailUI(C8oOnFail c8oOnFail);
    }
}
