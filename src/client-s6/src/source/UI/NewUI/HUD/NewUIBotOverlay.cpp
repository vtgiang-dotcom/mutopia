// NewUIBotOverlay.cpp: OpenMU Native Bot AI Companion Bar & Live Status Overlay.
#include "stdafx.h"
#include "UI/NewUI/HUD/NewUIBotOverlay.h"
#include "UI/NewUI/NewUISystem.h"
#include "I18N/All.h"
#include "Engine/Object/ZzzInterface.h"

namespace SEASON3B
{
    CNewUIBotOverlay::CNewUIBotOverlay()
    {
        m_pNewUIMng = nullptr;
        m_Pos.x = BOTOVERLAY_POS_X;
        m_Pos.y = BOTOVERLAY_POS_Y;
        m_iActiveBotCount = 0;
        wcscpy_s(m_szStatusText, L"OpenMU Smart Bot AI: Active");
    }

    CNewUIBotOverlay::~CNewUIBotOverlay()
    {
        Release();
    }

    bool CNewUIBotOverlay::Create(CNewUIManager* pNewUIMng, int x, int y)
    {
        if (pNewUIMng == nullptr) return false;

        m_pNewUIMng = pNewUIMng;
        m_pNewUIMng->AddUIObj(SEASON3B::INTERFACE_BOT_OVERLAY, this);

        SetPos(x, y);
        Show(true);
        return true;
    }

    void CNewUIBotOverlay::Release()
    {
        if (m_pNewUIMng)
        {
            m_pNewUIMng->RemoveUIObj(this);
            m_pNewUIMng = nullptr;
        }
    }

    void CNewUIBotOverlay::SetStatusText(const wchar_t* text)
    {
        if (text)
        {
            wcscpy_s(m_szStatusText, text);
        }
    }

    bool CNewUIBotOverlay::UpdateMouseEvent()
    {
        if (!IsVisible()) return true;

        if (CheckMouseIn(m_Pos.x, m_Pos.y, BOTOVERLAY_WIDTH, BOTOVERLAY_HEIGHT))
        {
            return false;
        }

        return true;
    }

    bool CNewUIBotOverlay::UpdateKeyEvent()
    {
        return true;
    }

    bool CNewUIBotOverlay::Update()
    {
        return true;
    }

    bool CNewUIBotOverlay::Render()
    {
        if (!IsVisible()) return true;

        EnableAlphaTest();
        glColor4f(0.0f, 0.0f, 0.0f, 0.6f); // Semi-transparent dark background

        // Render overlay card box
        RenderColor(m_Pos.x, m_Pos.y, BOTOVERLAY_WIDTH, BOTOVERLAY_HEIGHT);

        // Render border
        glColor4f(0.0f, 0.8f, 1.0f, 0.8f); // Cyan border
        RenderColor(m_Pos.x, m_Pos.y, BOTOVERLAY_WIDTH, 1);
        RenderColor(m_Pos.x, m_Pos.y + BOTOVERLAY_HEIGHT - 1, BOTOVERLAY_WIDTH, 1);
        RenderColor(m_Pos.x, m_Pos.y, 1, BOTOVERLAY_HEIGHT);
        RenderColor(m_Pos.x + BOTOVERLAY_WIDTH - 1, m_Pos.y, 1, BOTOVERLAY_HEIGHT);

        g_pRenderText->SetFont(g_hFontBold);
        g_pRenderText->SetTextColor(0, 255, 200, 255);
        g_pRenderText->SetBgColor(0, 0, 0, 0);
        g_pRenderText->RenderText(m_Pos.x + 8, m_Pos.y + 6, m_szStatusText, BOTOVERLAY_WIDTH - 16, 0, RT3_SORT_LEFT);

        wchar_t szBotCount[32];
        swprintf_s(szBotCount, L"Active Server Bots: %d", m_iActiveBotCount);
        g_pRenderText->SetFont(g_hFont);
        g_pRenderText->SetTextColor(255, 255, 255, 255);
        g_pRenderText->RenderText(m_Pos.x + 8, m_Pos.y + 22, szBotCount, BOTOVERLAY_WIDTH - 16, 0, RT3_SORT_LEFT);

        DisableAlphaBlend();
        return true;
    }
}
