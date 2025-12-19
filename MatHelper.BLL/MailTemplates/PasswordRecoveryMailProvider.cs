using MatHelper.CORE.Models;

namespace MatHelper.BLL.MailTemplates
{
    public static class PasswordRecoveryMailProvider
    {
        public static MailTemplate Get(string language, string mainLink, string recoveryLink)
        {
            return language switch
            {
                "ru" => GetRu(mainLink, recoveryLink),
                "uk" => GetUk(mainLink, recoveryLink),
                "fr" => GetFr(mainLink, recoveryLink),
                "de" => GetDe(mainLink, recoveryLink),
                "ja" => GetJa(mainLink, recoveryLink),
                "ko" => GetKo(mainLink, recoveryLink),
                "zh" => GetZh(mainLink, recoveryLink),
                _ => GetEn(mainLink, recoveryLink)
            };
        }

        private static MailTemplate GetEn(string mainLink, string recoveryLink) => new()
        {
            Subject = "Password Recovery",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Hello!</p>
                <p>We received a request to reset your password.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Reset Password</a>
                </p>
                <p>This link will expire in 15 minutes.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetRu(string mainLink, string recoveryLink) => new()
        {
            Subject = "Восстановление пароля",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Здравствуйте!</p>
                <p>Мы получили запрос на сброс вашего пароля.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Сбросить пароль</a>
                </p>
                <p>Ссылка действительна в течение 15 минут.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Все права защищены.</footer>
            </div>"
        };

        private static MailTemplate GetUk(string mainLink, string recoveryLink) => new()
        {
            Subject = "Відновлення пароля",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Вітаємо!</p>
                <p>Ми отримали запит на скидання вашого пароля.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Скинути пароль</a>
                </p>
                <p>Посилання дійсне протягом 15 хвилин.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Усі права захищені.</footer>
            </div>"
        };

        private static MailTemplate GetFr(string mainLink, string recoveryLink) => new()
        {
            Subject = "Récupération de mot de passe",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Bonjour&nbsp;!</p>
                <p>Nous avons reçu une demande de réinitialisation de votre mot de passe.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Réinitialiser le mot de passe</a>
                </p>
                <p>Ce lien expirera dans 15 minutes.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Tous droits réservés.</footer>
            </div>"
        };

        private static MailTemplate GetDe(string mainLink, string recoveryLink) => new()
        {
            Subject = "Passwort wiederherstellen",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Hallo!</p>
                <p>Wir haben eine Anfrage zum Zurücksetzen Ihres Passworts erhalten.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Passwort zurücksetzen</a>
                </p>
                <p>Dieser Link ist 15 Minuten gültig.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Alle Rechte vorbehalten.</footer>
            </div>"
        };

        private static MailTemplate GetJa(string mainLink, string recoveryLink) => new()
        {
            Subject = "パスワードの再設定",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>こんにちは！</p>
                <p>パスワード再設定のリクエストを受信しました。</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>パスワードをリセット</a>
                </p>
                <p>このリンクは15分間有効です。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetKo(string mainLink, string recoveryLink) => new()
        {
            Subject = "비밀번호 재설정",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>안녕하세요!</p>
                <p>비밀번호 재설정 요청을 받았습니다.</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>비밀번호 재설정</a>
                </p>
                <p>이 링크는 15분 동안 유효합니다.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetZh(string mainLink, string recoveryLink) => new()
        {
            Subject = "重置密码",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>你好！</p>
                <p>我们收到了重置您密码的请求。</p>
                <p style='text-align:center;'>
                    <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>重置密码</a>
                </p>
                <p>该链接将在15分钟后失效。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. 版权所有.</footer>
            </div>"
        };
    }
}
