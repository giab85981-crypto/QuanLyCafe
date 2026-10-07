using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class CustomerCatalog(AppDbContext db)
{
    public static string Phone(string? value)
    {
        var phone = Regex.Replace(value?.Trim() ?? "", @"[\s().-]", "");
        if (phone.StartsWith("+84")) phone = "0" + phone[3..];
        else if (phone.StartsWith("84") && phone.Length == 11) phone = "0" + phone[2..];
        return phone;
    }
    public async Task Validate(CreateCustomerDto dto, int? id = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 150 || dto.Phone == null || !Regex.IsMatch(Phone(dto.Phone), @"^0[0-9]{8,10}$")) throw new InvalidOperationException("Nhập tên và số điện thoại hợp lệ (9–11 số, bắt đầu bằng 0).");
        if (dto.Email == null || dto.Email.Length > 150 || (dto.Email.Trim() != "" && !new EmailAddressAttribute().IsValid(dto.Email.Trim())) || dto.Address == null || dto.Address.Length > 300 || dto.Note == null || dto.Note.Length > 500 || !new[] { "", "Nam", "Nữ", "Khác" }.Contains(dto.Gender)) throw new InvalidOperationException("Email hoặc thông tin khách hàng không hợp lệ.");
        if (dto.Birthday > DateTime.Today || dto.Birthday < new DateTime(1900, 1, 1)) throw new InvalidOperationException("Ngày sinh phải từ năm 1900 đến hôm nay.");
        if (dto.IdGroup.HasValue && !await db.CustomerGroups.AnyAsync(g => g.Id == dto.IdGroup)) throw new InvalidOperationException("Nhóm khách hàng không tồn tại.");
        var phone = Phone(dto.Phone);
        if (await db.Customers.AnyAsync(c => c.Id != id && c.Phone == phone)) throw new InvalidOperationException("Số điện thoại này đã thuộc khách hàng khác.");
    }
    public async Task<Customer> Save(CreateCustomerDto dto, Customer? customer = null)
    {
        await Validate(dto, customer?.Id);
        customer ??= new Customer();
        customer.Name = dto.Name.Trim(); customer.Phone = Phone(dto.Phone); customer.Email = dto.Email.Trim(); customer.Address = dto.Address.Trim(); customer.Note = dto.Note.Trim(); customer.Gender = dto.Gender; customer.Birthday = dto.Birthday?.Date; customer.IdGroup = dto.IdGroup;
        if (customer.Id == 0) db.Customers.Add(customer);
        await db.SaveChangesAsync();
        if (customer.Code == "") { customer.Code = $"KH{customer.Id:D6}"; await db.SaveChangesAsync(); }
        return customer;
    }
    public static CustomerDto Dto(Customer c) => new() { Id = c.Id, Code = c.Code, Name = c.Name, Phone = c.Phone, Points = c.Points };
}
