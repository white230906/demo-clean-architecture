using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using mini_1_helpdesk_ticket.Application.Common.Exceptions;

namespace mini_1_helpdesk_ticket.Application.Common;

public static partial class TextRules
{
    public static string Require(string? value, string fieldName)
    {
        var result = value?.Trim();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new AppException(
                ErrorType.Validation,
                "VALIDATION_FAILED",
                $"{fieldName} không được để trống.");
        }

        return result;
    }

    public static string ToSlug(string value)
    {
        var normalized = Require(value, "Name")
            .ToLowerInvariant()//Invarian: không phụ thuộc vào ngôn ngữ của hệ điều hành
            .Replace('đ', 'd')//unicode không xem chữ đ là chữ d có dấu, nên cần thay thủ công
            .Normalize(NormalizationForm.FormD);//tách kí tự có dấu thành kí tự và dấu riêng" ê -> e + ^
        //để xí nữa xóa dấu
        var builder = new StringBuilder();//tạo nơi để ghép chuỗi

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                //UnicodeCategory.NonSpacingMark: kí tự loại dấu
                builder.Append(character);
                //=> nếu kí tự khác dấu thì add vào builder
            }
        }
        
        var withoutMarks = builder.ToString().Normalize(NormalizationForm.FormC);
        //convert lại ngược thành FormC để unicode trở về trang thái bình thường
        //FormD: tách chữ và dấu để dễ xử lý (ở đây là xóa dấu).
        //FormC: ghép lại thành dạng Unicode chuẩn để lưu trữ và sử dụng tiếp. Đây là bước "dọn dẹp" sau khi xử lý xong chuỗi.
        return InvalidSlugCharacters()
            .Replace(withoutMarks, "-")
            .Trim('-');
        //Lúc này, InvalidSlugCharacters: là mộ regex, thằng này
        //nhận vào cái chuỗi string ở trên, và tiến hành thay thế những thằng nào
        //nó thuộc regex: tức là không phải chuỗi hoặc số, bằng giấy "-",
        //sau đó trim('-'), là cuối cùng ta có được slug hoàn chỉnh
    }
    //Tách các cụm không thuộc chữ hoặc số
    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidSlugCharacters();
}
