using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Jomura.Web.UI.WebControls
{
    /// <summary>
    /// TransactionToken�@�\��񋟂���B
    /// Redirect���APostback���ɕs���ȉ�ʑJ�ځA�{�^���Q�x���������o����B
    /// 
    /// ��ʂɔz�u����ƁA������ʕ\������Token�𐶐���Form�ɓo�^����B
    /// Redirect��̎���ʂ�Token�`�F�b�N�����{����ꍇ�ɂ́A
    /// RedirectUri��GET�p�����[�^�Ƃ���Token��t�^����K�v������B
    /// ���̍ہAGET�p�����[�^���́A����ʂ�Token�R���g���[����ID�Ɠ���Ƃ��邱�ƁB
    /// 
    /// �`�F�b�N�����ʂł́AIsTokenValid()�̖߂�l�������������K�v������B
    /// </summary>
    [ToolboxData("<{0}:Token runat=\"server\"></{0}:Token>")]
    public class Token : HiddenField
    {
        const string TOKEN_SESSION_NAME = "Jomura.Framework.token";

        /// <summary>
        /// get���AValue����Ȃ�Token�𐶐�����B
        /// </summary>
        public override string Value
        {
            get
            {
                if (string.IsNullOrEmpty(base.Value))
                {
                    base.Value = SaveToken(Context);
                }
                return base.Value;
            }
        }

        /// <summary>
        /// Token�̌����B
        /// 
        /// �ʏ�͕ύX�̕K�v�͂Ȃ����AURL����Z�k����ꍇ�ɂ́A
        /// ���v���p�e�B�Ő��l�����炷���Ƃ��\�BDefault��32���B
        /// ���Ȃ݂ɁAToken��16�i��������B
        /// </summary>
        [Bindable(true)]
        [Category("Format")]
        public short Figure
        {
            get
            {
                object obj = ViewState["Figure"];
                return obj == null ? (short)32 : (short)obj;
            }
            set
            {
                ViewState["Figure"] = value;
            }
        }

        /// <summary>
        /// Token�`�F�b�N���ʂ�Ԃ��B
        /// ��ʂ�Page_Load()�̍ŏ��ŗ��p����B
        /// Redirect���APostback���̗����ɗ��p�\�B
        /// 
        /// �܂��AGET(or Form)�p�����[�^���́A����Token�R���g���[����ID�Ɠ���ł��邱�ƁB
        /// 
        /// �{���\�b�h�����s�����Token���X�V�����B
        /// </summary>
        /// <param name="context">Http�R���e�L�X�g</param>
        /// <returns>Token�����������true, �s���Ȃ�false</returns>
        public bool IsTokenValid(HttpContext context)
        {
            string token = context.Request.Form[ID] ?? context.Request.QueryString[ID];
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("no token found for token name " + ID
                    + " -> Invalid token ");
                return false;
            }

            string sessionToken = context.Session[TOKEN_SESSION_NAME] as string;
            if (token != sessionToken)
            {
                Trace.TraceWarning("Form token {0} does not match the session token {1}.",
                    new object[] { token, sessionToken });
                return false;
            }

            // remove the token so it won't be used again
            //context.Session.Remove(TOKEN_SESSION_NAME);
            CreateToken(context);
            return true;
        }

        /// <summary>
        /// Token��V�K�������AForm��Session�ɓo�^����B
        /// �������A��|�X�g�o�b�N���̂݁B
        /// 
        /// Page_Load()��HyperLink.NavigateUrl����Token��t������ꍇ�́A
        /// ��ɂ��̃��\�b�h�����s���AToken�𐶐����Ă������ƁB
        /// </summary>
        /// <param name="context">Http�R���e�L�X�g</param>
        /// <returns>Token</returns>
        string SaveToken(HttpContext context)
        {
            //�|�X�g�o�b�N���ɂ́A�g�[�N����V�K�������Ȃ��B
            return Page.IsPostBack 
                ? context.Session[TOKEN_SESSION_NAME] as string
                : CreateToken(context);
        }

        string CreateToken(HttpContext context)
        {
            //Token����
            string token = Guid.NewGuid().ToString("N");
            if (32 > Figure && Figure > 0)
            {
                token = token.Substring(32 - Figure);
            }

            //Session�ɓo�^
            context.Session[TOKEN_SESSION_NAME] = token;
            //Request.Form�ɓo�^
            base.Value = token;
            
            return token;
        }

    }//eof class
}//eof namespace
