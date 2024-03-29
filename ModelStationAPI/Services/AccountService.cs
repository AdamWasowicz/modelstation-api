﻿using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ModelStationAPI.Entities;
using ModelStationAPI.Exceptions;
using ModelStationAPI.Interfaces;
using ModelStationAPI.Models;
using ModelStationAPI.Models.Account;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ModelStationAPI.Services
{
    public class AccountService : IAccountService
    {
        private readonly ModelStationDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public AccountService(ModelStationDbContext dbContext, IPasswordHasher<User> passwordHasher, 
            AuthenticationSettings authenticationSettings, IMapper mapper, IFileService fileService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _authenticationSettings = authenticationSettings;
            _mapper = mapper;
            _fileService = fileService;
        }

        public LoginResultDTO Login(LoginDTO dto)
        {
            var jwt = GenerateJwt(dto);
            var user = _dbContext
                .Users
                .Include(u => u.Role)
                    .Where(u => u.UserName == dto.UserName)
                        .FirstOrDefault();

            var userDTO = _mapper.Map<UserDTO>(user);

            var userFile = _dbContext
                .FilesStorage
                    .Where(u => u.UserId == userDTO.Id)
                    .Where(t => t.ContentType == "USER")
                        .FirstOrDefault();

            if (userFile != null)
            {
                var userFileDTO = _mapper.Map<FileStorageDTO>(userFile);
                userDTO.File = userFileDTO;
            }

            var loginResultDTO = new LoginResultDTO
            {
                user = userDTO,
                jwt = jwt
            };

            return loginResultDTO;
        }


        public int ChangePassword(ChangePasswordDTO dto, ClaimsPrincipal userClaims)
        {
            int userId = Convert.ToInt32(userClaims.FindFirst(c => c.Type == "UserId").Value);

            var user = _dbContext
                .Users
                    .Include(u => u.Role)
                        .Where(u => u.Id == userId)
                            .FirstOrDefault();

            if (user is null)
                return -1;

            
             var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
             if (result == PasswordVerificationResult.Failed)
                return -2;
            

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.LastEditDate = DateTime.Now;

            _dbContext.SaveChanges();

            return 0;
        }

        public int ChangeUserPassword(ChangePasswordDTO dto, ClaimsPrincipal userClaims)
        {
            var user = _dbContext
                .Users
                    .Include(u => u.Role)
                        .Where(u => u.UserName == dto.UserName)
                            .FirstOrDefault();

            if (user is null)
                return -1;


            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.LastEditDate = DateTime.Now;

            _dbContext.SaveChanges();

            return 0;
        }

        public int DeleteAccount(DeleteAccountDTO dto, ClaimsPrincipal userClaims)
        {
            int userId = Convert.ToInt32(userClaims.FindFirst(c => c.Type == "UserId").Value);

            var user = _dbContext
                .Users
                    .Include(u => u.Role)
                        .Where(u => u.Id == userId)
                            .FirstOrDefault();

            if (user is null)
                return -1;

            if (Convert.ToInt32(userClaims.FindFirst(c => c.Type == "AccessLevel").Value) < 10)
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
                if (result == PasswordVerificationResult.Failed)
                    return -2;
            }

            //Removal
            //LikedPost
            {
                var likedPosts = _dbContext
                    .LikedPosts
                        .Where(lp => lp.UserId == userId)
                            .ToList();

                _dbContext.RemoveRange(likedPosts);
                _dbContext.SaveChanges();
            }

            //LikedComments
            {
                var likedComments = _dbContext
                    .LikedComments
                        .Where(lc => lc.UserId == userId)
                            .ToList();

                _dbContext.LikedComments.RemoveRange(likedComments);
                _dbContext.SaveChanges();
            }

            //FileStorage
            {
                var files = _dbContext
                    .FilesStorage
                        .Where(fs => fs.UserId == userId)
                            .ToList();

                foreach (var f in files)
                    _fileService.Delete(f.Id, userClaims);

                _dbContext.SaveChanges();
            }

            //Comments
            {
                var comments = _dbContext
                    .Comments
                        .Where(c => c.UserId == userId)
                            .ToList();

                _dbContext.RemoveRange(comments);
                _dbContext.SaveChanges();
            }

            //Posts
            {
                var posts = _dbContext
                    .Posts
                        .Where(p => p.UserId == userId)
                            .ToList();


                foreach (var p in posts)
                {
                    //LikedPosts
                    var lps = _dbContext
                        .LikedPosts
                            .Where(lp => lp.PostId == p.Id)
                                .ToList();

                    _dbContext.RemoveRange(lps);
                    _dbContext.SaveChanges();


                    //Comments
                    var comments = _dbContext
                        .Comments
                            .Where(c => c.PostId == p.Id)
                                .ToList();

                    foreach (var c in comments)
                    {
                        //LikedComments
                        var lcs = _dbContext
                            .LikedComments
                                .Where(lc => lc.CommentId == c.Id)
                                    .ToList();

                        _dbContext.RemoveRange(lcs);
                        _dbContext.SaveChanges();
                    }
                }

                _dbContext.Posts.RemoveRange(posts);
                _dbContext.SaveChanges();            
            }


            _dbContext.Users.Remove(user);
            _dbContext.SaveChanges();

            return 0;
        }

        public string GenerateJwt(LoginDTO dto)
        {
            var user = _dbContext
                .Users
                .Include(u => u.Role)
                    .Where(u => u.UserName == dto.UserName)
                        .FirstOrDefault();

            if (user is null)
                throw new BadRequestException("Invalid username or password");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new BadRequestException("Invalid username or password");

            if (user.IsBanned == true)
                throw new UserBannedException("This user is banned");

            var test = user;

            var claims = new List<Claim>()
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("UserName", user.UserName.ToString()),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim("AccessLevel", user.Role.AccessLevel.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationSettings.JwtKey));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(_authenticationSettings.JwtExpireDays);

            var token = new JwtSecurityToken(_authenticationSettings.JwtIssuer,
                _authenticationSettings.JwtIssuer,
                claims,
                expires: expires,
                signingCredentials: cred
                );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
    }
}
