using MatHelper.CORE.Models;

namespace MatHelper.BLL.MailTemplates
{
	public static class IpConfirmationMailProvider
	{
		public static MailTemplate Get(string language, string mainLink, string code)
		{
			return language switch
			{
				"ru" => GetRu(mainLink, code),
				"uk" => GetUk(mainLink, code),
				"fr" => GetFr(mainLink, code),
				"de" => GetDe(mainLink, code),
				"ja" => GetJa(mainLink, code),
				"ko" => GetKo(mainLink, code),
				"zh" => GetZh(mainLink, code),
				_ => GetEn(mainLink, code)
			};
		}

        private static MailTemplate GetEn(string mainLink, string code) => new()
        {
            Subject = "New device login confirmation",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Hello,</p>
                <p>We detected a login attempt from a new device or IP address. To confirm this login, please enter the code below:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>This code will expire in 15 minutes. If you did not attempt to log in, please ignore this email.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetRu(string mainLink, string code) => new()
        {
            Subject = "Подтверждение входа с нового устройства",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Здравствуйте,</p>
                <p>Мы обнаружили попытку входа с нового устройства или IP-адреса. Для подтверждения входа введите код ниже:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>Код действителен в течение 15 минут. Если это были не вы — просто проигнорируйте письмо.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Все права защищены.</footer>
            </div>"
        };

        private static MailTemplate GetUk(string mainLink, string code) => new()
        {
            Subject = "Підтвердження входу з нового пристрою",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Вітаємо,</p>
                <p>Ми виявили спробу входу з нового пристрою або IP-адреси. Для підтвердження входу введіть код нижче:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>Код дійсний протягом 15 хвилин. Якщо це були не ви — проігноруйте цей лист.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Усі права захищені.</footer>
            </div>"
        };

        private static MailTemplate GetFr(string mainLink, string code) => new()
        {
            Subject = "Confirmation de connexion depuis un nouvel appareil",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Bonjour,</p>
                <p>Nous avons détecté une tentative de connexion depuis un nouvel appareil ou une nouvelle adresse IP. Pour confirmer la connexion, veuillez saisir le code ci-dessous :</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>Ce code expirera dans 15 minutes. Si vous n’êtes pas à l’origine de cette tentative, ignorez cet e-mail.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Tous droits réservés.</footer>
            </div>"
        };

        private static MailTemplate GetDe(string mainLink, string code) => new()
        {
            Subject = "Anmeldung von neuem Gerät bestätigen",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>Hallo,</p>
                <p>Wir haben einen Anmeldeversuch von einem neuen Gerät oder einer neuen IP-Adresse festgestellt. Bitte geben Sie den folgenden Code ein, um die Anmeldung zu bestätigen:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>Der Code ist 15 Minuten gültig. Falls Sie dies nicht waren, ignorieren Sie bitte diese E-Mail.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. Alle Rechte vorbehalten.</footer>
            </div>"
        };

        private static MailTemplate GetJa(string mainLink, string code) => new()
        {
            Subject = "新しいデバイスからのログイン確認",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>こんにちは、</p>
                <p>新しいデバイスまたはIPアドレスからのログイン試行が検出されました。確認のため、以下のコードを入力してください：</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>このコードは15分間有効です。心当たりがない場合は、このメールを無視してください。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetKo(string mainLink, string code) => new()
        {
            Subject = "새 기기 로그인 확인",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>안녕하세요,</p>
                <p>새로운 기기 또는 IP 주소에서 로그인 시도가 감지되었습니다. 로그인을 확인하려면 아래 코드를 입력하세요:</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>이 코드는 15분 동안 유효합니다. 본인이 아니라면 이 이메일을 무시하세요.</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. All rights reserved.</footer>
            </div>"
        };

        private static MailTemplate GetZh(string mainLink, string code) => new()
        {
            Subject = "新设备登录确认",
            Body = $@"
            <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                <h2 style='text-align:center;'>
                    <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                        <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                    </a>
                </h2>
                <p>你好，</p>
                <p>我们检测到来自新设备或新 IP 地址的登录尝试。请输入以下验证码以确认登录：</p>
                <p style='text-align:center; font-size:24px; font-weight:bold; color:#C444FF;'>{code}</p>
                <p>该验证码将在 15 分钟后失效。如果这不是您的操作，请忽略此邮件。</p>
                <hr style='border-color:#444;'/>
                <footer style='text-align:center; font-size:12px; color:#666;'>&copy; {DateTime.Now.Year} GMHelper. 版权所有.</footer>
            </div>"
        };
	}
}
