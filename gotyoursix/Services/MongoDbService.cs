using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using static gotyoursix.Data.DBContext;
using BCrypt.Net;
using static gotyoursix.Data.CommonClasses;
using System.Security.Cryptography;
using MongoDB.Bson;

namespace gotyoursix.Services
{

    public class MongoDbService 
    {

        private readonly IMongoCollection<Users> _usersCollection;
        private readonly IMongoCollection<PasswordResetToken> _tokensCollection;
        private readonly IMongoCollection<RegistrationConfirmToken> _registrationTokensCollection;

        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public MongoDbService(string connectionString, string databaseName, IPasswordHasher<Users> passwordHasher)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);

            //initialize the collections
            _usersCollection = database.GetCollection<Users>("Users");
            _tokensCollection = database.GetCollection<PasswordResetToken>("PasswordResetTokens");
            _registrationTokensCollection = database.GetCollection<RegistrationConfirmToken>("RegistrationConfirmTokens");
        }




        #region Users
        public async Task<bool> RegisterUserAsync(Users newUser, string password)
        {
            // Hash the password before storing (similar to Identity)
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Check if the user already exists
            var existingUser = await _usersCollection.Find(u => u.Email == newUser.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return false; // User already exists
            }

            // Insert the new user into the database
            await _usersCollection.InsertOneAsync(newUser);
            return true; // Registration successful
        }


        public async Task<LoginReturn> ValidateUserAsync(string email, string password)
        {
            var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            LoginReturn returnValue = new LoginReturn();

            if (user == null)
            {
                returnValue.Result = false;
                returnValue.Description = "Email address is not registered";
                return returnValue; 
            }

            bool pwHashResult = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!pwHashResult)
            {
                returnValue.Result = false;
                returnValue.Description = "Email and password does not match";
            }
            else
            {
                returnValue.Result = true;
            }

            return returnValue;


        }

        public async Task<Users> GetUser(string email)
        {
            var user = await _usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

            return user;
        }

        public async Task UpdateUser(Users user)
        {
            var filter = Builders<Users>.Filter.Eq(c => c.Email, user.Email);
            var update = Builders<Users>.Update.Set(c => c.RegistrationConfirmed, user.RegistrationConfirmed)
                                                            .Set(c => c.FirstName, user.FirstName)
                                                            .Set(c => c.LastName, user.LastName);

            await _usersCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateUserWithPassword(Users user, string password)
        {

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var filter = Builders<Users>.Filter.Eq(c => c.Email, user.Email);
            var update = Builders<Users>.Update.Set(c => c.RegistrationConfirmed, user.RegistrationConfirmed)
                                                            .Set(c => c.FirstName, user.FirstName)
                                                            .Set(c => c.LastName, user.LastName)
                                                            .Set(c => c.PasswordHash, user.PasswordHash);


            var result = await _usersCollection.UpdateOneAsync(filter, update);

        }
        #endregion


        #region Token
        public async Task<string> GenerateResetTokenAsync(string email)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure random token
            var expiry = DateTime.UtcNow.AddHours(1); // 1-hour expiration

            var resetToken = new PasswordResetToken
            {
                Email = email,
                Token = token,
                Expiry = expiry
            };

            await _tokensCollection.InsertOneAsync(resetToken);
            return token;
        }

        // Validate the token and reset the password
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var tokenRecord = await _tokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();

            if (tokenRecord == null || tokenRecord.Expiry < DateTime.UtcNow)
                return false; // Invalid or expired token

            var update = Builders<Users>.Update.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(newPassword));
            await _usersCollection.UpdateOneAsync(u => u.Email == tokenRecord.Email, update);

            // Remove the used token
            await _tokensCollection.DeleteOneAsync(t => t.Token == token);
            return true;
        }

        public async Task<string> GenerateRegisterTokenAsync(string email)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Secure random token

            var resetToken = new RegistrationConfirmToken
            {
                Email = email,
                Token = token,
            };

            await _registrationTokensCollection.InsertOneAsync(resetToken);
            return token;
        }

        // Validate the registration token
        public async Task<bool> ValidateGeneratedRegisterTokenAsync(string token)
        {
            var tokenRecord = await _registrationTokensCollection.Find(t => t.Token == token).FirstOrDefaultAsync();

            if (tokenRecord == null)
                return false; // Invalid token

            // Remove the used token
            await _registrationTokensCollection.DeleteOneAsync(t => t.Token == token);
            return true;
        }
    }
    #endregion
}

