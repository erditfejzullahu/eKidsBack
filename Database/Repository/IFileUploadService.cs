namespace Database.Repository
{
    public interface IFileUploadService
    {
        Task<string> UploadFile(string base64Data, FileCategory category);
    }
}
