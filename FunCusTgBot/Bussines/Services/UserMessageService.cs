using DataAccess.Entities;
using DataAccess.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Bussines.Services
{
    public class UserMessageService : IUserMessageService
    {
        private readonly IUserMessageRepository _messageRepository;

        public UserMessageService(IUserMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task UpdateUserMessageAsync(long id, string newText)
        {
            var message = await _messageRepository.GetLastMessageAsync(id);
            if (message != null)
            {
                message.Text = newText;
                message.SendDate = DateTime.UtcNow;

                await _messageRepository.UpdateMessageAsync(message);
                Console.WriteLine($"Сообщение с ID {id} обновлено: {newText}");
            }
            else
            {
                throw new KeyNotFoundException($"Сообщение с ID {id} не найдено.");
            }
        }

        public async Task AddMessageAsync(UserMessage message)
        {
            var messageEntity = new UserMessageEntity
            {
                ChatId = message.ChatId,
                MessageId = message.MessageId,
                Text = message.Text,
                SendDate = DateTime.UtcNow
            };

            await _messageRepository.AddMessageAsync(messageEntity);
        }

        public async Task<UserMessage> GetLastMessageAsync(long userId)
        {
            var messageEntity = await _messageRepository.GetLastMessageAsync(userId);

            if (messageEntity is null)
            {
                return null;
            }

            var message = new UserMessage
            {
                ChatId = messageEntity.ChatId,
                MessageId = messageEntity.MessageId,
                Text = messageEntity.Text,
                SendDate = messageEntity.SendDate
            };

            return message;
        }

        public async Task UpdateMessageAsync(UserMessage message)
        {
            var messageEntity = await _messageRepository.GetLastMessageAsync(message.ChatId);
            if (messageEntity != null)
            {
                messageEntity.MessageId = message.MessageId;
                messageEntity.Text = message.Text;
                messageEntity.SendDate = message.SendDate;

                await _messageRepository.UpdateMessageAsync(messageEntity);
            }
            else
            {
                throw new KeyNotFoundException($"Сообщение с ID {message.MessageId} не найдено.");
            }
        }
    }

}
