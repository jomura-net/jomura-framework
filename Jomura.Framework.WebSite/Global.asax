<%@ Application Language="C#" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e) 
    {
        // アプリケーションのスタートアップで実行するコードです

    }
    
    void Application_End(object sender, EventArgs e) 
    {
        //  アプリケーションのシャットダウンで実行するコードです

    }
        
    void Application_Error(object sender, EventArgs e) 
    { 
        // ハンドルされていないエラーが発生したときに実行するコードです

        Exception ex = Server.GetLastError();
        if (ex == null)
        {
            //例外が取得できなかった場合は、何も処理しない
            return;
        }

        ex = Server.GetLastError().GetBaseException();
        if (ex is HttpException)
        {
            //HttpExceptionの場合は、処理しない
            Response.StatusCode = ((HttpException)ex).GetHttpCode();
            if (Response.StatusCode == 500)
            {
                //TODO 内部サーバーエラーをログに記録する
            }
            Response.End();
            return;
        }

        //TODO エラーログ出力
        
#if ERROR_ALL_CLEAR
        //エラーページに例外詳細を表示しない場合は、ここでエラーをクリア
        Server.ClearError();
#else
        //エラーページに例外詳細を表示する場合は、エラーページ側でクリアする。
#endif

        //Redirect()かExecute()か、どちらかを選択する。
#if ERROR_REDIRECT
        Response.Redirect("~/Error.aspx");
#else
        Server.Execute("~/Error.aspx");
#endif
    }

    void Session_Start(object sender, EventArgs e) 
    {
        // 新規セッションを開始したときに実行するコードです

    }

    void Session_End(object sender, EventArgs e) 
    {
        // セッションが終了したときに実行するコードです 
        // メモ: Web.config ファイル内で sessionstate モードが InProc に設定されているときのみ、
        // Session_End イベントが発生します。session モードが StateServer か、または SQLServer に 
        // 設定されている場合、イベントは発生しません。

    }
       
</script>
