using System;

namespace MES.Solution.Models
{
    /// <summary>
    /// 현재 로그인한 사용자의 정보를 담는 모델 클래스
    /// </summary>
    public class CurrentUser
    {
        /// <summary>
        /// 데이터베이스의 고유 사용자 ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 사용자 이메일
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 사용자 역할 (ADMIN 또는 USER)
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// 로그인 시간
        /// </summary>
        public DateTime LoggedInTime { get; set; }

        /// <summary>
        /// 관리자 여부를 반환하는 속성
        /// </summary>
        public bool IsAdmin => RoleName?.ToUpper() == "ADMIN";

        /// <summary>
        /// 빈 사용자 객체를 반환하는 정적 속성
        /// </summary>
        public static CurrentUser Empty => new CurrentUser
        {
            UserId = 0,
            Username = string.Empty,
            Email = string.Empty,
            RoleName = string.Empty,
            LoggedInTime = DateTime.MinValue
        };

        /// <summary>
        /// 사용자 정보를 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"User: {Username} (ID: {UserId}, Role: {RoleName})";
        }
    }
}