﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using LinCms.Core.Common;
using LinCms.Core.Data.Enums;
using LinCms.Core.Entities;

namespace LinCms.Application.Cms.Users
{
    public class UserIdentityService : IUserIdentityService
    {
        private readonly IFreeSql _freeSql;
        public UserIdentityService(IFreeSql freeSql)
        {
            _freeSql = freeSql;
        }

        /// <summary>
        /// 记录授权成功后的信息
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="openId"></param>
        /// <returns></returns>
        public long SaveGitHub(ClaimsPrincipal principal, string openId)
        {
            string email = principal.FindFirst(ClaimTypes.Email)?.Value;
            string name = principal.FindFirst(ClaimTypes.Name)?.Value;
            string gitHubName = principal.FindFirst(GitHubAuthenticationConstants.Claims.Name)?.Value;
            string gitHubApiUrl = principal.FindFirst(GitHubAuthenticationConstants.Claims.Url)?.Value;
            string avatarUrl = principal.FindFirst(LinConsts.Claims.AvatarUrl)?.Value;
            string bio = principal.FindFirst(LinConsts.Claims.BIO)?.Value;
            string blogAddress = principal.FindFirst(LinConsts.Claims.BlogAddress)?.Value;
            Expression<Func<LinUserIdentity, bool>> expression = r => r.IdentityType == LinUserIdentity.GitHub && r.Credential == openId;

            LinUserIdentity linUserIdentity = _freeSql.Select<LinUserIdentity>().Where(expression).First();

            long userId = 0;
            if (linUserIdentity == null)
            {
                userId = _freeSql.Insert(new LinUser
                {
                    Admin = (int)UserAdmin.Common,
                    Active = (int)UserActive.Active,
                    Avatar = avatarUrl,
                    CreateTime = DateTime.Now,
                    Email = email,
                    Introduction = bio,
                    LinUserGroups = new List<LinUserGroup>()
                    {
                        new LinUserGroup()
                        {
                            GroupId = LinConsts.Group.User
                        }
                    },

                    Nickname = gitHubName,
                    Username = email,
                    BlogAddress = blogAddress,
                    LinUserIdentitys = new List<LinUserIdentity>()
                    {
                        new LinUserIdentity
                        {
                            CreateTime = DateTime.Now,
                            Credential = openId,
                            IdentityType = LinUserIdentity.GitHub,
                            Identifier = name,
                            CreateUserId = userId
                        }
                    }
                }).ExecuteIdentity();
            }
            else
            {
                userId = linUserIdentity.CreateUserId;

                //_freeSql.Update<LinUserIdentity>(linUserIdentity.Id).Set(r => new LinUserIdentity()
                //{
                //    BlogAddress = blogAddress,
                //}).ExecuteAffrows();
            }

            return userId;

        }

        public bool VerifyUsernamePassword(long userId, string username, string password)
        {
            LinUserIdentity userIdentity = _freeSql.Select<LinUserIdentity>()
                .Where(r => r.CreateUserId == userId && r.Identifier == username)
                .ToOne();

            return userIdentity != null && EncryptUtil.Verify(userIdentity.Credential, password);
        }

        public async Task ChangePasswordAsync(long userId, string newpassword)
        {
            string encryptPassword = EncryptUtil.Encrypt(newpassword);

            await _freeSql.Update<LinUser>(userId).Set(a => new LinUser()
            {
                Password = encryptPassword
            }).ExecuteAffrowsAsync();
        }
    }
}