using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if KK || KKS
using Studio;
#endif


namespace BrowserFolders
{
    public static class BrowserFoldersUtil
    {
#if KK || KKS
        public static void ScrollToSelected(SceneLoadScene sls)
        {
            if (sls == null)
                return;
            
            //Save page value to a local variable as it may be initialized by KK
            int page = Mathf.Clamp(SceneLoadScene.page, 0, sls.pageNum - 1);

            sls.DelayedInvoke(() => {
                var scroll = sls.GetComponentInChildren<UnityEngine.UI.Scrollbar>();
                if (scroll != null)
                    scroll.value = 1.0f - (float)page / (sls.pageNum - 1);

#if KK
                //Someone will reset it.
                sls.SetPage(page);
#endif
            }, 2);
        }
#endif

    }
}

