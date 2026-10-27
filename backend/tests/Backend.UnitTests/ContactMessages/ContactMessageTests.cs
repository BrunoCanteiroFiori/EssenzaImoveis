using Backend.Domain.Common;
using Backend.Domain.ContactMessages;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.ContactMessages;

public sealed class ContactMessageTests
{
    [Fact]
    public void Create_WithValidData_ShouldNormalizeContact()
    {
        var contactMessage = ContactMessage.Create(null, " João ", " JOAO@Email.com ", "(11) 98765-4321", "Tenho interesse.", TestData.Now);

        Assert.Equal("João", contactMessage.Name);
        Assert.Equal("joao@email.com", contactMessage.Email);
        Assert.Equal("11987654321", contactMessage.Phone);
        Assert.Equal(ContactMessageStatus.New, contactMessage.Status);
    }

    [Fact]
    public void Create_WithoutEmailAndPhone_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => ContactMessage.Create(null, "João", null, " ", "Mensagem", TestData.Now));
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => ContactMessage.Create(null, "João", "email-invalido", null, "Mensagem", TestData.Now));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345678901234")]
    public void Create_WithInvalidPhone_ShouldThrowDomainException(string phone)
    {
        Assert.Throws<DomainException>(() => ContactMessage.Create(null, "João", null, phone, "Mensagem", TestData.Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyMessage_ShouldThrowDomainException(string message)
    {
        Assert.Throws<DomainException>(() => ContactMessage.Create(null, "João", "joao@email.com", null, message, TestData.Now));
    }

    [Fact]
    public void ChangeStatus_BackToNew_ShouldThrowDomainException()
    {
        var contactMessage = ContactMessage.Create(null, "João", "joao@email.com", null, "Mensagem", TestData.Now);
        contactMessage.ChangeStatus(ContactMessageStatus.Answered, TestData.Now);

        Assert.Throws<DomainException>(() => contactMessage.ChangeStatus(ContactMessageStatus.New, TestData.Now));
    }

    [Fact]
    public void UpdateInternalNotes_WithBlankNotes_ShouldClearNotes()
    {
        var contactMessage = ContactMessage.Create(null, "João", "joao@email.com", null, "Mensagem", TestData.Now);
        contactMessage.UpdateInternalNotes("Ligar amanhã", TestData.Now);

        contactMessage.UpdateInternalNotes("  ", TestData.Now.AddHours(1));

        Assert.Null(contactMessage.InternalNotes);
        Assert.Equal(TestData.Now.AddHours(1), contactMessage.UpdatedAt);
    }
}
