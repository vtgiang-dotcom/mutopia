// NewUIRuudShop.h: interface for Priest James Ruud Merchant Store UI.
#ifndef AFX_NEWUIRUUDSHOP_H_INCLUDED
#define AFX_NEWUIRUUDSHOP_H_INCLUDED

#pragma once

#include "UI/NewUI/NewUIBase.h"
#include "UI/NewUI/Inventory/NewUIInventoryCtrl.h"
#include "UI/NewUI/Dialogs/NewUIMessageBox.h"
#include "UI/NewUI/Inventory/NewUIMyInventory.h"
#include "UI/NewUI/Widgets/NewUIButton.h"

namespace SEASON3B
{
    class CNewUIRuudShop : public CNewUIObj
    {
    public:
        enum IMAGE_LIST
        {
            IMAGE_RUUDSHOP_BACK = CNewUIMessageBoxMng::IMAGE_MSGBOX_BACK,
            IMAGE_RUUDSHOP_TOP = CNewUIMyInventory::IMAGE_INVENTORY_BACK_TOP2,
            IMAGE_RUUDSHOP_LEFT = CNewUIMyInventory::IMAGE_INVENTORY_BACK_LEFT,
            IMAGE_RUUDSHOP_RIGHT = CNewUIMyInventory::IMAGE_INVENTORY_BACK_RIGHT,
            IMAGE_RUUDSHOP_BOTTOM = CNewUIMyInventory::IMAGE_INVENTORY_BACK_BOTTOM,
        };

        enum
        {
            RUUDSHOP_POS_X = 260,
            RUUDSHOP_POS_Y = 0,
            RUUDSHOP_WIDTH = 190,
            RUUDSHOP_HEIGHT = 429,
        };

    private:
        CNewUIManager* m_pNewUIMng;
        CNewUIInventoryCtrl* m_pNewInventoryCtrl;
        POINT m_Pos;
        bool m_bIsOpen;

    public:
        CNewUIRuudShop();
        virtual ~CNewUIRuudShop();

        bool Create(CNewUIManager* pNewUIMng, int x, int y);
        void Release();

        void SetPos(int x, int y);
        const POINT& GetPos() const { return m_Pos; }

        bool UpdateMouseEvent();
        bool UpdateKeyEvent();
        bool Update();
        bool Render();

        float GetLayerDepth() { return 2.2f; }

        void OpenRuudShop();
        void CloseRuudShop();
        bool IsRuudShopOpen() const { return m_bIsOpen; }
    };
}

#endif // AFX_NEWUIRUUDSHOP_H_INCLUDED
