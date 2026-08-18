// NewUIRuudShop.cpp: implementation for Priest James Ruud Merchant Store UI.
#include "stdafx.h"
#include "UI/NewUI/NPCs/NewUIRuudShop.h"
#include "UI/NewUI/NewUISystem.h"
#include "I18N/All.h"
#include "Engine/Object/ZzzInterface.h"

namespace SEASON3B
{
    CNewUIRuudShop::CNewUIRuudShop()
    {
        m_pNewUIMng = nullptr;
        m_pNewInventoryCtrl = nullptr;
        m_Pos.x = m_Pos.y = 0;
        m_bIsOpen = false;
    }

    CNewUIRuudShop::~CNewUIRuudShop()
    {
        Release();
    }

    bool CNewUIRuudShop::Create(CNewUIManager* pNewUIMng, int x, int y)
    {
        if (pNewUIMng == nullptr) return false;

        m_pNewUIMng = pNewUIMng;
        m_pNewUIMng->AddUIObj(SEASON3B::INTERFACE_RUUD_SHOP, this);

        SetPos(x, y);

        m_pNewInventoryCtrl = new CNewUIInventoryCtrl();
        if (m_pNewInventoryCtrl->Create(STORAGE_TYPE::UNDEFINED, g_pNewUI3DRenderMng, g_pNewItemMng, this, x + 15, y + 50, 8, 15))
        {
            m_pNewInventoryCtrl->SetToolTipType(TOOLTIP_TYPE_NPC_SHOP);
        }


        Show(false);
        return true;
    }

    void CNewUIRuudShop::Release()
    {
        SAFE_DELETE(m_pNewInventoryCtrl);
        if (m_pNewUIMng)
        {
            m_pNewUIMng->RemoveUIObj(this);
            m_pNewUIMng = nullptr;
        }
    }

    void CNewUIRuudShop::SetPos(int x, int y)
    {
        m_Pos.x = x;
        m_Pos.y = y;
        if (m_pNewInventoryCtrl)
        {
            m_pNewInventoryCtrl->SetPos(x + 15, y + 50);
        }
    }

    bool CNewUIRuudShop::UpdateMouseEvent()
    {
        if (!IsVisible()) return true;

        if (m_pNewInventoryCtrl && m_pNewInventoryCtrl->UpdateMouseEvent())
        {
            return false;
        }

        if (CheckMouseIn(m_Pos.x, m_Pos.y, RUUDSHOP_WIDTH, RUUDSHOP_HEIGHT))
        {
            return false;
        }

        return true;
    }

    bool CNewUIRuudShop::UpdateKeyEvent()
    {
        if (!IsVisible()) return true;

        if (IsPress(VK_ESCAPE))
        {
            g_pNewUISystem->Hide(SEASON3B::INTERFACE_RUUD_SHOP);
            return false;
        }

        return true;
    }

    bool CNewUIRuudShop::Update()
    {
        if (!IsVisible()) return true;

        if (m_pNewInventoryCtrl)
        {
            m_pNewInventoryCtrl->Update();
        }

        return true;
    }

    bool CNewUIRuudShop::Render()
    {
        if (!IsVisible()) return true;

        EnableAlphaTest();
        glColor4f(1.0f, 1.0f, 1.0f, 1.0f);

        // Render Ruud Shop Background Window
        RenderImage(IMAGE_RUUDSHOP_TOP, m_Pos.x, m_Pos.y, RUUDSHOP_WIDTH, 50);
        RenderImage(IMAGE_RUUDSHOP_LEFT, m_Pos.x, m_Pos.y + 50, 20, RUUDSHOP_HEIGHT - 100);
        RenderImage(IMAGE_RUUDSHOP_RIGHT, m_Pos.x + RUUDSHOP_WIDTH - 20, m_Pos.y + 50, 20, RUUDSHOP_HEIGHT - 100);
        RenderImage(IMAGE_RUUDSHOP_BOTTOM, m_Pos.x, m_Pos.y + RUUDSHOP_HEIGHT - 50, RUUDSHOP_WIDTH, 50);

        g_pRenderText->SetFont(g_hFontBold);
        g_pRenderText->SetTextColor(255, 215, 0, 255); // Gold Title
        g_pRenderText->SetBgColor(0, 0, 0, 0);
        g_pRenderText->RenderText(m_Pos.x + 40, m_Pos.y + 15, L"Priest James (Ruud Shop)", 110, 0, RT3_SORT_CENTER);

        if (m_pNewInventoryCtrl)
        {
            m_pNewInventoryCtrl->Render();
        }

        DisableAlphaBlend();
        return true;
    }

    void CNewUIRuudShop::OpenRuudShop()
    {
        m_bIsOpen = true;
        Show(true);
    }

    void CNewUIRuudShop::CloseRuudShop()
    {
        m_bIsOpen = false;
        Show(false);
    }
}
