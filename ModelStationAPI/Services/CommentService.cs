﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ModelStationAPI.Entities;
using ModelStationAPI.Exceptions;
using ModelStationAPI.Interfaces;
using ModelStationAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelStationAPI.Services
{
    public class CommentService : ICommentService
    {
        private readonly ModelStationDbContext _dbContext;
        private readonly IMapper _mapper;

        public CommentService(ModelStationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public List<CommentDTO> GetAll()
        {
            var comments = _dbContext
                .Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .ToList();

            var commentsDTO = _mapper.Map<List<CommentDTO>>(comments);
            return commentsDTO;
        }

        public CommentDTO GetById(int id)
        {
            var comment = _dbContext
                .Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefault();

            if (comment == null)
                throw new NotFoundException("There is no comment with that Id");

            var commentDTO = _mapper.Map<CommentDTO>(comment);
            return commentDTO;
        }

        public int Create(CreateCommentDTO dto)
        {
            var comment = _mapper.Map<Comment>(dto);

            comment.IsActive = true;
            comment.IsBanned = false;
            comment.CreationDate = DateTime.Now;
            comment.LastEditDate = comment.CreationDate;
            comment.Likes = 0;

            _dbContext.Comments.Add(comment);
            _dbContext.SaveChanges();

            return comment.Id;

        }

        public bool Delete(int id)
        {
            var comment = _dbContext
                .Comments
                .FirstOrDefault(c => c.Id == id);

            if (comment is null)
                throw new NotFoundException("Comment with that Id does not exist");

            _dbContext.Comments.Remove(comment);
            _dbContext.SaveChanges();

            return true;
        }

        public List<CommentDTO> GetCommentsByPostId(int postId)
        {
            var comments = _dbContext
                .Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .Where(c => c.PostId == postId)
                .ToList();

            var commentsDTO = _mapper.Map<List<CommentDTO>>(comments);
            return commentsDTO;
        }

        public List<CommentDTO> GetCommentsByUserId(int userId)
        {
            var comments = _dbContext
                .Comments
                .Include(c => c.User)
                .Include(c => c.Post)
                .Where(c => c.UserId == userId)
                .ToList();

            var commentsDTO = _mapper.Map<List<CommentDTO>>(comments);
            return commentsDTO;
        }
    }
}