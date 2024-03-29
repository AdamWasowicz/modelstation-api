﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelStationAPI.Entities;
using ModelStationAPI.Models;
using AutoMapper;
using ModelStationAPI.Services;
using ModelStationAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ModelStationAPI.Controllers
{
    [Route("api/v1/postcategory")]
    [ApiController]
    public class PostCategoryController : Controller
    {
        private readonly IPostCategoryService _postCategoryService;

        public PostCategoryController(IPostCategoryService postCategoryService)
        {
            _postCategoryService = postCategoryService;
        }

        [HttpGet]
        public ActionResult<List<PostCategoryDTO>> GetAll()
        {
            var postCategoriesDTO = _postCategoryService.GetAll();
            return postCategoriesDTO;
        }

        [HttpGet("{id}")]
        public ActionResult<PostCategoryDTO> GetById([FromRoute] int id)
        {
            var postCategoryDTO = _postCategoryService.GetById(id);
            return postCategoryDTO;
        }

        [HttpPost]
        public ActionResult CreatePostCategory([FromBody] CreatePostCategoryDTO dto)
        {
            //Check if model is valid
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int createdId = _postCategoryService.Create(dto);
            return Created(createdId.ToString(), null);
        }

        [HttpDelete]
        public ActionResult DeletePostCategory([FromRoute] int id)
        {
            bool isDeleted = _postCategoryService.Delete(id);

            if (isDeleted)
                return NoContent();

            return NotFound();
        }

        [HttpPatch]
        public ActionResult EditPostCategory([FromBody] EditPostCategoryDTO dto)
        {
            //Check if model is valid
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool result = _postCategoryService.Edit(dto);

            if (result)
                return Ok();

            return NotFound();
        }
    }
}
