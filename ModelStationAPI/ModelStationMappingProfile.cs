﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ModelStationAPI.Entities;
using ModelStationAPI.Models;

namespace ModelStationAPI
{
    public class ModelStationMappingProfile : Profile
    {
        //Mapping Profiles
        public ModelStationMappingProfile()
        {
            //Post
            //Post to ...
            CreateMap<Post, PostDTO>()
                .ForMember(m => m.UserId, c => c.MapFrom(s => s.User.Id))
                .ForMember(m => m.UserName, c => c.MapFrom(s => s.User.UserName))
                .ForMember(m => m.PostCategoryId, c => c.MapFrom(s => s.PostCategory.Id))
                .ForMember(m => m.PostCategoryName, c => c.MapFrom(s => s.PostCategory.Name));
            CreateMap<Post, PostBannerDTO>()
                .ForMember(m => m.User_UserName, c => c.MapFrom(s => s.User.UserName))
                .ForMember(m => m.PostCategory_Name, c => c.MapFrom(s => s.PostCategory.Name));
            //CreatePostDTO to Post
            CreateMap<CreatePostDTO, Post>();
            CreateMap<CreatePostWithPostCategoryNameDTO, Post>();


            //Comment
            //Comment to CommentDTO
            CreateMap<Comment, CommentDTO>()
                .ForMember(m => m.UserId, c => c.MapFrom(s => s.User.Id))
                .ForMember(m => m.UserName, c => c.MapFrom(s => s.User.UserName))
                .ForMember(m => m.PostId, c => c.MapFrom(s => s.Post.Id))
                .ForMember(m => m.UserImageId, c => c.MapFrom(s => s.User.FileStorageId));
            //CreateCommentDTO to Comment
            CreateMap<CreateCommentDTO, Comment>();


            //User
            CreateMap<User, UserDTO>();
            CreateMap<User, UserBannerDTO>();
            CreateMap<User, UserProfileDTO>();
            CreateMap<CreateUserDTO, User>();


            //PostCategory
            //PostCategory to PostCategoryDTO
            CreateMap<PostCategory, PostCategoryDTO>();
            //CreatePostCategoryDTO to PostCategory
            CreateMap<CreatePostCategoryDTO, PostCategory>();


            //LikedPost
            //LikedPost to LikedPostDTO
            CreateMap<LikedPost, LikedPostDTO>();
            //CreateLikedPostDTO to LikedPost
            CreateMap<CreateLikedPostDTO, LikedPost>();
            //CreateOrEditLikedPostDTO
            CreateMap<CreateOrEditLikedPostDTO, LikedPost>();


            //LikedComment
            CreateMap<LikedComment, LikedCommentDTO>();
            CreateMap<CreateLikedCommentDTO, LikedComment>();
            CreateMap<CreateOrEditLikedCommentDTO, LikedComment>();


            //FileStorage
            //FileStorage to FileStorageDTO
            CreateMap<FileStorage, FileStorageDTO>();
        }
    }
}
