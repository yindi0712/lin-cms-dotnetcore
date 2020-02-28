﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LinCms.Application.Cms.Admin;
using LinCms.Application.Cms.Users;
using LinCms.Application.Contracts.Cms.Admins;
using LinCms.Application.Contracts.Cms.Permissions;
using LinCms.Application.Contracts.Cms.Users;
using LinCms.Core.Aop;
using LinCms.Core.Data;
using LinCms.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LinCms.Web.Controllers.Cms
{
    [Route("cms/admin")]
    [ApiController]
    [LinCmsAuthorize(Roles = LinGroup.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userSevice;
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;
        public AdminController(IUserService userSevice, IAdminService adminService, IMapper mapper)
        {
            _userSevice = userSevice;
            _adminService = adminService;
            _mapper = mapper;
        }

        /// <summary>
        /// 用户信息分页列表项
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        [HttpGet("users")]
        public PagedResultDto<UserDto> GetUserListByGroupId([FromQuery]UserSearchDto searchDto)
        {
            return _userSevice.GetUserListByGroupId(searchDto);
        }


        /// <summary>
        /// 新增用户-不是注册，注册不可能让用户选择gourp_id
        /// </summary>
        /// <param name="userInput"></param>
        [AuditingLog("管理员新建了一个用户")]
        [HttpPost("register")]
        [LinCmsAuthorize(Roles = LinGroup.Admin)]
        public UnifyResponseDto Post([FromBody] CreateUserDto userInput)
        {
            _userSevice.Register(_mapper.Map<LinUser>(userInput), userInput.GroupIds);

            return UnifyResponseDto.Success("用户创建成功");
        }
        /// <summary>
        /// 修改用户信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateUserDto"></param>
        /// <returns></returns>
        [HttpPut("user/{id}")]
        public UnifyResponseDto Put(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            _userSevice.UpdateUserInfo(id, updateUserDto);
            return UnifyResponseDto.Success();
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuditingLog("管理员删除了一个用户")]
        [HttpDelete("{id}")]
        public async Task<UnifyResponseDto> DeleteAsync(int id)
        {
            await _userSevice.DeleteAsync(id);
            return UnifyResponseDto.Success("删除用户成功");
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <param name="id">用户id</param>
        /// <param name="resetPasswordDto"></param>
        /// <returns></returns>
        [HttpPut("password/{id}")]
        public async Task<UnifyResponseDto> ResetPasswordAsync(int id, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            await _userSevice.ResetPasswordAsync(id, resetPasswordDto);
            return UnifyResponseDto.Success("密码修改成功");
        }

        /// <summary>
        /// 查询所有可分配的权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("permission")]
        public IDictionary<string, List<PermissionDto>> GetAllPermissions()
        {
            return _adminService.GetAllStructualPermissions();
        }
    }
}