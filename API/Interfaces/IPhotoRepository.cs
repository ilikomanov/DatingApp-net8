using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IPhotoRepository
{
    Task<IEnumerable<PhotoForApprovalDto>> GetUnapprovedPhotos();
    Task<Photo?> GetPhotoById(int id);
    void RemovePhoto(Photo photo);
    void RemovePhotos(List<Photo> photos);
}
