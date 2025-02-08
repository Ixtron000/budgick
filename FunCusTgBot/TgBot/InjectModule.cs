﻿using Autofac;
using Bussines.Services;
using DataAccess;
using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Repositories;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TgBot.Extensions;

namespace TgBot
{
    public class InjectModule : Module
    {
        private readonly string _connectionString;
        private readonly string _telegramBotToken;

        public InjectModule()
        {
            _connectionString = AppExtensions.GetConnectionString();
            _telegramBotToken = AppExtensions.GetTelegramBotToken();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Регистрируем TelegramBotClient как Singleton
            builder.RegisterInstance(new TelegramBotClient(_telegramBotToken)).As<TelegramBotClient>().SingleInstance();

            // Регистрируем appdbcontext
            builder.Register(c =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));

                return new AppDbContext(optionsBuilder.Options);
            }).AsSelf().InstancePerLifetimeScope();

            // регистрация базового репозитория
            builder.RegisterGeneric(typeof(BaseRepository<>))
               .As(typeof(IBaseRepository<>))
               .InstancePerLifetimeScope();

            builder.RegisterType<UserRepository>().As<IUserRepository>().InstancePerLifetimeScope();

            // Регистрируем сервисы
            builder.RegisterType<BotClientService>().As<IBotClientService>().InstancePerLifetimeScope();

        }
    }
}
