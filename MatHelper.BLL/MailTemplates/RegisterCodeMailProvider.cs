using MatHelper.CORE.Models;

namespace MatHelper.BLL.MailTemplates
{
    public static class RegisterCodeMailProvider
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

        private static MailTemplate GetEn(string mainLink, string code) => new()
        {
            Subject = "Email confirmation for registration",
            Body = $@"
    <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
        <h2 style='text-align:center;'>
            <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
            </a>
        </h2>
        <p>Hello,</p>
        <p>To complete your registration, please enter the verification code below:</p>
        <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
        <p>This code will expire in 5 minutes. If you did not request registration, you can ignore this email.</p>
        <hr style='border-color:#444;'/>
        <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
    </div>"
        };

        private static MailTemplate GetRu(string mainLink, string code) => new()
        {
            Subject = "Код подтверждения регистрации",
            Body = $@"
    <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
        <h2 style='text-align:center;'>
            <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
            </a>
        </h2>
        <p>Здравствуйте,</p>
        <p>Для завершения регистрации введите код подтверждения ниже:</p>
        <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
        <p>Код действует 5 минут. Если вы не регистрировались — проигнорируйте это письмо.</p>
        <hr style='border-color:#444;'/>
        <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Все права защищены.</footer>
    </div>"
        };

        private static MailTemplate GetUk(string mainLink, string code) => new()
        {
            Subject = "Код підтвердження реєстрації",
            Body = $@"
    <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
        <h2 style='text-align:center;'>
            <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
            </a>
        </h2>
        <p>Вітаємо,</p>
        <p>Щоб завершити реєстрацію, введіть код підтвердження нижче:</p>
        <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
        <p>Код дійсний 5 хвилин. Якщо це були не ви — проігноруйте лист.</p>
        <hr style='border-color:#444;'/>
        <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Усі права захищені.</footer>
    </div>"
        };

        private static MailTemplate GetFr(string mainLink, string code) => new()
        {
            Subject = "Code de confirmation d’inscription",
            Body = $@"
    <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
        <h2 style='text-align:center;'>
            <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
            </a>
        </h2>
        <p>Bonjour,</p>
        <p>Pour finaliser votre inscription, veuillez entrer le code ci-dessous :</p>
        <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
        <p>Ce code est valable 5 minutes. Si vous n’êtes pas à l’origine de cette demande, ignorez cet email.</p>
        <hr style='border-color:#444;'/>
        <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Tous droits réservés.</footer>
    </div>"
        };

        private static MailTemplate GetDe(string mainLink, string code) => new()
        {
            Subject = "Registrierungsbestätigungscode",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Hallo,</p>
                <p>Um die Registrierung abzuschließen, geben Sie bitte den folgenden Code ein:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>Dieser Code ist 5 Minuten gültig. Falls Sie sich nicht registriert haben, ignorieren Sie diese E-Mail.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Alle Rechte vorbehalten.</footer>
            </div>"
        };

        private static MailTemplate GetJa(string mainLink, string code) => new()
        {
            Subject = "登録確認コード",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>こんにちは、</p>
                <p>登録を完了するために、以下のコードを入力してください：</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>このコードは5分間有効です。心当たりがない場合は無視してください。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetKo(string mainLink, string code) => new()
        {
            Subject = "회원가입 인증 코드",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>안녕하세요,</p>
                <p>회원가입을 완료하려면 아래 인증 코드를 입력하세요:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>이 코드는 5분 동안 유효합니다. 본인이 아니라면 이 이메일을 무시하세요.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetZh(string mainLink, string code) => new()
        {
            Subject = "注册验证码",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>你好，</p>
                <p>请输入以下验证码以完成注册：</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>验证码5分钟内有效。如果不是您操作，请忽略此邮件。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. 版权所有.</footer>
            </div>"
        };
    }
}
