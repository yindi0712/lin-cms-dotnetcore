﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using CSRedis;
using FreeSql;
using FreeSql.Internal;
using LinCms.Application.Cms.Files;
using LinCms.Application.Contracts.Cms.Files;
using LinCms.Core.Entities;
using LinCms.Core.Middleware;
using LinCms.Core.Security;
using LinCms.Web.Data.Authorization;
using LinCms.Web.Middleware;
using LinCms.Web.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog.Web;
using ToolGood.Words;

namespace LinCms.Web.Configs
{
    public static class DependencyInjectionExtensions
    {
        #region FreeSql
        /// <summary>
        /// FreeSql
        /// </summary>
        /// <param name="services"></param>
        public static void AddContext(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            IConfigurationSection configurationSection = configuration.GetSection("ConnectionStrings:MySql");

            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            IFreeSql fsql = new FreeSqlBuilder()
                   .UseConnectionString(DataType.MySql, configurationSection.Value)
                   .UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
                   .UseAutoSyncStructure(true)
                   .UseMonitorCommand(cmd =>
                       {
                           Trace.WriteLine(cmd.CommandText + ";");
                       }
                   )
                   .Build()
                   .SetDbContextOptions(opt => opt.EnableAddOrUpdateNavigateList = true);//联级保存功能开启（默认为关闭）



            fsql.Aop.CurdAfter += (s, e) =>
            {
                logger.Debug($"ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}: FullName:{e.EntityType.FullName}" +
                             $" ElapsedMilliseconds:{e.ElapsedMilliseconds}ms, {e.Sql}");
            };

            //敏感词处理
            if (configuration["AuditValue:Enable"].ToBoolean())
            {
                IllegalWordsSearch illegalWords = ToolGoodUtils.GetIllegalWordsSearch();

                fsql.Aop.AuditValue += (s, e) =>
                {
                    if (e.Column.CsType == typeof(string) && e.Value != null)
                    {
                        string oldVal = (string)e.Value;
                        string newVal = illegalWords.Replace(oldVal);
                        //第二种处理敏感词的方式
                        //string newVal = oldVal.ReplaceStopWords();
                        if (newVal != oldVal)
                        {
                            e.Value = newVal;
                        }
                    }
                };
            }

            services.AddSingleton(fsql);
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IFreeSql>().CreateUnitOfWork());

            Expression<Func<IDeleteAduitEntity, bool>> where = a => a.IsDeleted == false;
            fsql.GlobalFilter.Apply("IsDeleted", where);

            services.AddFreeRepository();
        }
        #endregion

        public static void AddCsRedisCore(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            #region 初始化 Redis配置
            IConfigurationSection csRediSection = configuration.GetSection("ConnectionStrings:CsRedis");
            CSRedisClient csredis = new CSRedisClient(csRediSection.Value);
            //初始化 RedisHelper
            RedisHelper.Initialization(csredis);
            //注册mvc分布式缓存
            services.AddSingleton<IDistributedCache>(new CSRedisCache(RedisHelper.Instance));
            #endregion
        }

        public static void AddDIServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddTransient<CustomExceptionMiddleWare>();
            services.AddTransient<UnitOfWorkMiddleware>();

            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            string serviceName = configuration.GetSection("FILE:SERVICE").Value;
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException("FILE:SERVICE未配置");
            if (serviceName == LinFile.LocalFileService)
            {
                services.AddTransient<IFileService, LocalFileService>();
            }
            else
            {
                services.AddTransient<IFileService, QiniuService>();
            }
        }


        public static IServiceCollection AddFreeRepository(this IServiceCollection services)
        {
            services.TryAddTransient(typeof(IBaseRepository<>), typeof(GuidRepository<>));
            services.TryAddTransient(typeof(BaseRepository<>), typeof(GuidRepository<>));
            services.TryAddTransient(typeof(IBaseRepository<,>), typeof(DefaultRepository<,>));
            services.TryAddTransient(typeof(BaseRepository<,>), typeof(DefaultRepository<,>));
            return services;
        }
    }
}