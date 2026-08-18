// NewUIBotOverlay.h: OpenMU Native Bot AI Companion Bar & Live Status Overlay.
#ifndef AFX_NEWUIBOTOVERLAY_H_INCLUDED
#define AFX_NEWUIBOTOVERLAY_H_INCLUDED

#pragma once

#include "UI/NewUI/NewUIBase.h"
#include "UI/NewUI/NewUIManager.h"

namespace SEASON3B
{
    class CNewUIBotOverlay : public CNewUIObj
    {
    public:
        enum
        {
            BOTOVERLAY_POS_X = 10,
            BOTOVERLAY_POS_Y = 100,
            BOTOVERLAY_WIDTH = 150,
            BOTOVERLAY_HEIGHT = 45,
        };

    private:
        CNewUIManager* m_pNewUIMng;
        POINT m_Pos;
        int m_iActiveBotCount;
        wchar_t m_szStatusText[64];

    public:
        CNewUIBotOverlay();
        virtual ~CNewUIBotOverlay();

        bool Create(CNewUIManager* pNewUIMng, int x, int y);
        void Release();

        void SetPos(int x, int y) { m_Pos.x = x; m_Pos.y = y; }
        const POINT& GetPos() const { return m_Pos; }

        bool UpdateMouseEvent();
        bool UpdateKeyEvent();
        bool Update();
        bool Render();

        float GetLayerDepth() { return 1.5f; }

        void SetActiveBotCount(int count) { m_iActiveBotCount = count; }
        void SetStatusText(const wchar_t* text);
    };
}

#endif // AFX_NEWUIBOTOVERLAY_H_INCLUDED
