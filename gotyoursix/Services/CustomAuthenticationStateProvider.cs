using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Maui.Storage;
using System.Security.Claims;
using System.Threading.Tasks;

namespace gotyoursix.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        // This method is used to get the current authentication state
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Fetch the stored email and session expiration time from SecureStorage
                var storedEmail = await SecureStorage.GetAsync("userEmail");
                var storedExpiration = await SecureStorage.GetAsync("sessionExpiration");

                if (!string.IsNullOrEmpty(storedEmail) && DateTime.TryParse(storedExpiration, out var expirationTime))
                {
                    if (DateTime.UtcNow < expirationTime)
                    {
                        // If the session is valid, create a ClaimsIdentity and set the current user
                        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, storedEmail) }, "auth");
                        _currentUser = new ClaimsPrincipal(identity);
                    }
                }
            }
            catch
            {
                // In case of an error, set the user as unauthenticated
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }

            return new AuthenticationState(_currentUser);
        }

        // This method is used to mark the user as authenticated
        public async Task MarkUserAsAuthenticated(string email)
        {
            // Create the ClaimsIdentity for the authenticated user
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }, "auth");
            _currentUser = new ClaimsPrincipal(identity);

            // Set the session expiration time to 48 hours from the current time
            var expirationTime = DateTime.UtcNow.AddHours(48);
            await SecureStorage.SetAsync("userEmail", email);
            await SecureStorage.SetAsync("sessionExpiration", expirationTime.ToString("o"));

            // Notify the system that the authentication state has changed
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // This method is used to log the user out
        public async Task MarkUserAsLoggedOut()
        {
            // Clear the current user
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

            // Remove user details and session expiration from SecureStorage
            SecureStorage.Remove("userEmail");   // Using Remove instead of RemoveAsync
            SecureStorage.Remove("sessionExpiration");   // Using Remove instead of RemoveAsync

            // Notify the system that the authentication state has changed
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        // Expose the logout method
        public Task LogoutAsync() => MarkUserAsLoggedOut();
    }
}
