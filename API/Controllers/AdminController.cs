using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork,
     IPhotoService photoService) : BaseApiController
{
    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await userManager.Users
            .OrderBy(x => x.UserName)
            .Select(x => new
            {
                x.Id,
                Username = x.UserName,
                Roles = x.UserRoles.Select(r => r.Role.Name).ToList()
            }).ToListAsync();

        return Ok(users);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("edit-roles/{username}")]
    public async Task<ActionResult> EditRoles(string username, string roles)
    {
        if (string.IsNullOrEmpty(roles)) return BadRequest("You must select at least one role");

        var selectedRoles = roles.Split(",").ToArray();

        var user = await userManager.FindByNameAsync(username);

        if (user == null) return BadRequest("User not found");

        var userRoles = await userManager.GetRolesAsync(user);

        var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded) return BadRequest("Failed to add to roles");

        result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded) return BadRequest("Failed to remove from roles");

        return Ok(await userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photos-to-moderate")]
    public async Task<ActionResult> GetPhotosForModeration()
    {
        var photos = await unitOfWork.PhotoRepository.GetUnapprovedPhotos();

        return Ok(photos);
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("approve-photo/{photoId}")]
    public async Task<ActionResult> ApprovePhoto(int photoId)
    {
        var photo = await unitOfWork.PhotoRepository.GetPhotoById(photoId);

        if (photo == null) return BadRequest("Could not get photo from db");

        photo.IsApproved = true;

        var user = await unitOfWork.UserRepository.GetUserByPhotoId(photoId);

        if (user == null) return BadRequest("Could not get user from db");

        if (!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

        await unitOfWork.Complete();

        return Ok();
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("reject-photo/{photoId}")]
    public async Task<ActionResult> RejectPhoto(int photoId)
    {
        var photo = await unitOfWork.PhotoRepository.GetPhotoById(photoId);

        if (photo == null) return BadRequest("Could not get photo from db");

        if (photo.PublicId != null)
        {
            var result = await photoService.DeletePhotoAsync(photo.PublicId);

            if (result.Result == "ok")
            {
                unitOfWork.PhotoRepository.RemovePhoto(photo);


            }
        }
        else
        {
            unitOfWork.PhotoRepository.RemovePhoto(photo);
        }

        await unitOfWork.Complete();

        return Ok();
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpDelete("delete-user/{username}")]
    public async Task<ActionResult> DeleteUser(string username)
    {
        var user = await userManager.Users
            .Include(u => u.Photos)
            .Include(u => u.LikedByUsers)
            .Include(u => u.LikedUsers)
            .Include(u => u.MessagesSent)
            .Include(u => u.MessagesReceived)
            .FirstOrDefaultAsync(u => u.UserName == username.ToLower());

        if (user == null) return NotFound("User not found");

        // Remove associated photos
        if (user.Photos.Any())
            unitOfWork.PhotoRepository.RemovePhotos(user.Photos.ToList());

        // Remove likes and messages
        unitOfWork.LikesRepository.RemoveUserLikes(user.Id);
        unitOfWork.MessageRepository.RemoveUserMessages(user.Id);

        // Remove from roles
        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Any())
            await userManager.RemoveFromRolesAsync(user, userRoles);

        // Delete the user via UserManager
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest("Failed to delete user");

        await unitOfWork.Complete();

        return Ok("User deleted successfully");
    }
}
