﻿using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using LinCms.Web.Models.v1.Articles;
using LinCms.Zero.Domain.Blog;
using LinCms.Zero.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinCms.Test.Controller.v1
{
    public class ArticleControllerTest : BaseControllerTests
    {
        private readonly IHostingEnvironment _hostingEnv;
        private readonly IMapper _mapper;
        private readonly IFreeSql _freeSql;
        private readonly AuditBaseRepository<Article> _articleRepository;

        public ArticleControllerTest() : base()
        {
            _hostingEnv = serviceProvider.GetService<IHostingEnvironment>();

            _mapper = serviceProvider.GetService<IMapper>();
            _articleRepository = serviceProvider.GetService<AuditBaseRepository<Article>>();
            _freeSql = serviceProvider.GetService<IFreeSql>();
        }



        /// <summary>
        /// 使用BaseItem某一类别作为文件分类专栏时，使用From来join出分类专栏的名称。
        /// </summary>
        [Fact]
        public void Gets()
        {
            //不使用关联属性获取文章专栏
            List<ArticleDto> articleDtos = _articleRepository
                .Select
                .From<Classify>((a, b) =>
                    a.LeftJoin(r => r.ClassifyId == b.Id)
                ).ToList((s, a) => new
                {
                    Article = s,
                    a.ClassifyName
                })
                .Select(r =>
                {
                    ArticleDto articleDto = _mapper.Map<ArticleDto>(r.Article);
                    articleDto.ClassifyName = r.ClassifyName;
                    return articleDto;
                }).ToList();

            //使用SQL直接获取文章及其分类名称
            List<ArticleDto> t9 = _freeSql.Ado.Query<ArticleDto>($@"
                            SELECT a.*,b.item_name as classifyName 
                            FROM blog_article a 
                            LEFT JOIN base_item b 
                            on a.classify_id=b.id where a.is_deleted=0"
            );

            //属性Classify为null
            List<Article> articles1 = _articleRepository
                .Select
                .ToList();

            //属性Classify会有值
            List<Article>articles2=  _articleRepository
                .Select
                .Include(r => r.Classify)
                .ToList();

            //配合IMapper，转换为ArticleDto
            List<ArticleDto> articles3 = _articleRepository
                .Select
                .ToList(r=>new
                {
                    r.Classify,
                    Article=r
                }).Select(r=>
                {
                    ArticleDto articleDto=_mapper.Map<ArticleDto>(r.Article);
                    articleDto.ClassifyName = r.Classify.ClassifyName;
                    return articleDto;
                }).ToList();


            List<ArticleDto> articles4 = _articleRepository
                .Select
                .Include(r => r.Classify)
                .ToList().Select(r =>
                {
                    ArticleDto articleDto = _mapper.Map<ArticleDto>(r);
                    articleDto.ClassifyName = r.Classify?.ClassifyName;
                    return articleDto;
                }).ToList();


        }

    }
}
