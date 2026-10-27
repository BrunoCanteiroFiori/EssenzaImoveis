using Backend.Application.Common;
using Backend.Application.ContactMessages;
using Backend.Application.ContactMessages.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.ContactMessages;
using Backend.Domain.Properties;
using Backend.UnitTests.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Backend.UnitTests.ContactMessages;

public sealed class ContactMessageServiceTests
{
    private readonly IContactMessageRepository _contactMessageRepository = Substitute.For<IContactMessageRepository>();
    private readonly IPropertyRepository _propertyRepository = Substitute.For<IPropertyRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.Now);
    private readonly ContactMessageService _contactMessageService;

    public ContactMessageServiceTests()
    {
        _contactMessageService = new ContactMessageService(_contactMessageRepository, _propertyRepository, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistMessage()
    {
        var property = TestData.Property();
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        var request = new CreateContactMessageRequest(property.Id, "João", "joao@email.com", null, "Quero visitar.");

        var result = await _contactMessageService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _contactMessageRepository.Received(1).AddAsync(
            Arg.Is<ContactMessage>(contactMessage => contactMessage.PropertyId == property.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithHoneypotFilled_ShouldIgnoreMessage()
    {
        var request = new CreateContactMessageRequest(null, "Robô", "robo@spam.com", null, "Spam", Website: "http://spam.com");

        var result = await _contactMessageService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnavailableProperty_ShouldReturnValidationError()
    {
        var property = TestData.Property();
        property.ChangeStatus(PropertyStatus.Inactive, TestData.Now);
        _propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);
        var request = new CreateContactMessageRequest(property.Id, "João", "joao@email.com", null, "Mensagem");

        var result = await _contactMessageService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WithoutContact_ShouldReturnValidationError()
    {
        var request = new CreateContactMessageRequest(null, "João", null, null, "Mensagem");

        var result = await _contactMessageService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithMessages_ShouldIncludePropertyTitle()
    {
        var propertyId = Guid.CreateVersion7();
        var contactMessage = ContactMessage.Create(propertyId, "João", "joao@email.com", null, "Mensagem", TestData.Now);
        _contactMessageRepository
            .SearchAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedList<ContactMessage>([contactMessage], 1, 1, 20));
        _propertyRepository
            .GetTitlesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { [propertyId] = "Casa com quintal" });

        var page = await _contactMessageService.ListAsync(new ListContactMessagesRequest());

        Assert.Equal("Casa com quintal", Assert.Single(page.Items).PropertyTitle);
    }

    [Fact]
    public async Task UpdateAsync_BackToNew_ShouldReturnValidationError()
    {
        var contactMessage = ContactMessage.Create(null, "João", "joao@email.com", null, "Mensagem", TestData.Now);
        contactMessage.ChangeStatus(ContactMessageStatus.Answered, TestData.Now);
        _contactMessageRepository.GetByIdAsync(contactMessage.Id, Arg.Any<CancellationToken>()).Returns(contactMessage);

        var result = await _contactMessageService.UpdateAsync(contactMessage.Id, new UpdateContactMessageRequest(ContactMessageStatus.New, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldPersistStatusAndNotes()
    {
        var contactMessage = ContactMessage.Create(null, "João", "joao@email.com", null, "Mensagem", TestData.Now);
        _contactMessageRepository.GetByIdAsync(contactMessage.Id, Arg.Any<CancellationToken>()).Returns(contactMessage);

        var result = await _contactMessageService.UpdateAsync(
            contactMessage.Id,
            new UpdateContactMessageRequest(ContactMessageStatus.Answered, "Retornado por telefone"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ContactMessageStatus.Answered, result.Value.Status);
        Assert.Equal("Retornado por telefone", result.Value.InternalNotes);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
