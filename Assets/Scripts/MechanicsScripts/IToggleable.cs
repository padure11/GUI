public interface IToggleable
{
    void SetActive(bool active);
    bool IsActive { get; }
}
