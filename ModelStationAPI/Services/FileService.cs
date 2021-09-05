﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ModelStationAPI.Entities;
using ModelStationAPI.Exceptions;
using ModelStationAPI.Interfaces;
using ModelStationAPI.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ModelStationAPI.Services
{
    public class FileService : IFileService
    { 
        private readonly ModelStationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly string fileStoragePath = "Files/FileStorage";

        public FileService(ModelStationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        private string HashName(CreateFileStorageDTO dto)
        {
            string toBeHashedString = dto.UserId + dto.File.FileName + DateTime.Now.ToString();
            using (var sha = new SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(toBeHashedString);
                byte[] hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        public int Upload(CreateFileStorageDTO dto)
        {
            IFormFile file = dto.File;

            if (file != null && file.Length > 0)
            {
                var rootPath = Directory.GetCurrentDirectory();
                var fileName = HashName(dto);
                var fullPath = $"{rootPath}/{fileStoragePath}/{fileName}.{file.ContentType}";
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }


                //User
                var user = _dbContext
                    .Users
                    .Where(u => u.Id == dto.UserId)
                    .FirstOrDefault();

                if (user == null)
                    throw new NotFoundException("There is no User with that Id");


                //REPLACE
                if (dto.ContentType == "USER")
                    DeletePreviousUserImage(dto);


                FileStorage newFile = new FileStorage()
                {
                    ContentType = dto.ContentType,
                    PostId = (dto.PostId != 0 && dto.PostId != null) ? dto.PostId : 0,
                    FileType = file.ContentType,
                    UserGivenName = dto.File.FileName,
                    StorageName = fileName,
                    FullName = fileName + "." + file.ContentType,
                    FullPath = fullPath,
                    UploadDate = DateTime.Now,
                    UserId = dto.UserId
                };



                _dbContext.FilesStorage.Add(newFile);

                user.FileStorageId = newFile.Id;

                _dbContext.SaveChanges();

                return newFile.Id;
            }
            else
                throw new BadRequestException("There is something wrong with the file");
        }

        public bool Delete(int id, int userId)
        {
            var fileStorage = _dbContext
                .FilesStorage
                .Where(fs => fs.Id == id)
                .FirstOrDefault();

            if (fileStorage.UserId != userId)
                throw new UnauthorizedAccessException("This user has no acces to that file");

            if (fileStorage == null)
                throw new NotFoundException("There is no FileStorage with that Id");

            string path = fileStorage.FullPath;

            if (!File.Exists(path))
                throw new FileNotFoundException();

            File.Delete(path);
            if (File.Exists(path))
                throw new Exception("File could not be deleted");

            return true;
        }

        public Byte[] GetFileByFileStorageId(int fileStorageId)
        {
            var file = _dbContext
                .FilesStorage
                    .Where(fs => fs.Id == fileStorageId)
                        .FirstOrDefault();

            if (!File.Exists(file.FullPath))
                throw new NotFoundException("There is no file with that Id");

            var fileContent = File.ReadAllBytes(file.FullPath);
            return fileContent;
        }

        public Byte[] GetFileByByFileStorageName(string storageName)
        {
            var file = _dbContext
                .FilesStorage
                    .Where(fs => fs.StorageName == storageName)
                        .FirstOrDefault();

            if (!File.Exists(file.FullPath))
                throw new NotFoundException("There is no file with that Id");

            var fileContent = File.ReadAllBytes(file.FullPath);
            return fileContent;
        }

        public FileStorageDTO GetById(int id)
        {
            var file = _dbContext
                .FilesStorage
                    .Where(fs => fs.Id == id)
                        .FirstOrDefault();

            var fileDTO = _mapper.Map<FileStorageDTO>(file);
            return fileDTO;
        }

        public List<FileStorageDTO> GetFilesByPostId(int postId)
        {
            var postExist = _dbContext
                .Posts
                .Where(p => p.Id == postId)
                .Any();

            if (!postExist)
                throw new NotFoundException("There is no post with that Id");


            var files = _dbContext
                .FilesStorage
                    .Where(ls => ls.ContentType == "POST")
                    .Where(ls => ls.PostId == postId)
                        .ToList();

            var filesDTO = _mapper.Map<List<FileStorageDTO>>(files);
            return filesDTO;
        }

        public FileStorageDTO GetUserImage(int userId)
        {
            var fileStorage = _dbContext
                .FilesStorage
                .Where(fs => fs.ContentType == "USER")
                .Where(fs => fs.UserId == userId)
                .FirstOrDefault();

            if (fileStorage == null)
                throw new NotFoundException("This user does not have profile photo");

            var retFile = GetById(fileStorage.Id);
            return retFile;
        }

        private void DeletePreviousUserImage(CreateFileStorageDTO dto)
        {
            var user = _dbContext
                .Users
                .Where(u => u.Id == dto.UserId)
                .FirstOrDefault();

            if (user == null)
                return;

            var userImage = _dbContext
                .FilesStorage
                .Where(fs => fs.UserId == dto.UserId)
                .FirstOrDefault();

            if (userImage == null)
                return;

            File.Delete(userImage.FullPath);

            user.FileStorageId = null;

            _dbContext.FilesStorage.Remove(userImage);

            _dbContext.SaveChanges();
        }
    }
}
