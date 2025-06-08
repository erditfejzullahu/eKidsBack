using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public enum ToastType
    {
        ShareItemNotification = 0,
        MessageNotification = 1
    }



    public class ToastResponseDto
    {
        public string ToastTitle { get; set; }
        public string? ToastContent { get; set; }
        public ToastType ToastType { get; set; }    
        public string? Image { get; set; }
    }
}
